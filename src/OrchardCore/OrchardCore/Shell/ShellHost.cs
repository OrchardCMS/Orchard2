using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Descriptor.Models;
using OrchardCore.Environment.Shell.Events;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;

namespace OrchardCore.Environment.Shell
{
    /// <summary>
    /// All <see cref="ShellContext"/> are pre-created when <see cref="InitializeAsync"/> is called on startup and where we first load
    /// all <see cref="ShellSettings"/> that we also need to register in the <see cref="IRunningShellTable"/> to serve incoming requests.
    /// For each <see cref="ShellContext"/> a service container and then a request pipeline are only built on the first matching request.
    /// </summary>
    public class ShellHost : IShellHost, IDisposable
    {
        private const int ReloadShellMaxRetriesCount = 9;

        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IShellContextFactory _shellContextFactory;
        private readonly IRunningShellTable _runningShellTable;
        private readonly IExtensionManager _extensionManager;
        private readonly ILogger _logger;
        private readonly IOptions<ShellOptions> _shellOptions;

        private bool _initialized;
        private readonly ConcurrentDictionary<string, ShellContext> _shellContexts = new ConcurrentDictionary<string, ShellContext>();
        private readonly ConcurrentDictionary<string, ShellSettings> _shellSettings = new ConcurrentDictionary<string, ShellSettings>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _shellSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private SemaphoreSlim _initializingSemaphore = new SemaphoreSlim(1);

        public ShellHost(
            IShellSettingsManager shellSettingsManager,
            IShellContextFactory shellContextFactory,
            IRunningShellTable runningShellTable,
            IExtensionManager extensionManager,
            ILogger<ShellHost> logger,
            IOptions<ShellOptions> shellOptions)
        {
            _shellSettingsManager = shellSettingsManager;
            _shellContextFactory = shellContextFactory;
            _runningShellTable = runningShellTable;
            _extensionManager = extensionManager;
            _logger = logger;
            _shellOptions = shellOptions;
        }

        public ShellsEvent LoadingAsync { get; set; }
        public ShellEvent ReleasingAsync { get; set; }
        public ShellEvent ReloadingAsync { get; set; }

        public async Task InitializeAsync()
        {
            if (!_initialized)
            {
                // Prevent concurrent requests from creating all shells multiple times
                await _initializingSemaphore.WaitAsync();
                try
                {
                    if (!_initialized)
                    {
                        await PreCreateAndRegisterShellsAsync();
                    }
                }
                finally
                {
                    _initialized = true;
                    _initializingSemaphore.Release();
                }
            }
        }

        public async Task<ShellContext> GetOrCreateShellContextAsync(ShellSettings settings)
        {
            ShellContext shell = null;

            while (shell == null)
            {
                if (!_shellContexts.TryGetValue(settings.Name, out shell))
                {
                    var semaphore = _shellSemaphores.GetOrAdd(settings.Name, (name) => new SemaphoreSlim(1));

                    await semaphore.WaitAsync();

                    try
                    {
                        if (!_shellContexts.TryGetValue(settings.Name, out shell))
                        {
                            shell = await CreateShellContextAsync(settings);
                            AddAndRegisterShell(shell);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                if (shell.Released)
                {
                    // If the context is released, it is removed from the dictionary so that the next iteration
                    // or a new call on 'GetOrCreateShellContextAsync()' will recreate a new shell context.
                    _shellContexts.TryRemove(settings.Name, out _);
                    shell = null;
                }
            }

            return shell;
        }

        public async Task<ShellScope> GetScopeAsync(ShellSettings settings)
        {
            ShellScope scope = null;

            while (scope == null)
            {
                if (!_shellContexts.TryGetValue(settings.Name, out var shellContext))
                {
                    shellContext = await GetOrCreateShellContextAsync(settings);
                }

                // We create a scope before checking if the shell has been released.
                scope = shellContext.CreateScope();

                // If CreateScope() returned null, the shell is released. We then remove it and
                // retry with the hope to get one that won't be released before we create a scope.
                if (scope == null)
                {
                    // If the context is released, it is removed from the dictionary so that the next
                    // iteration or a new call on 'GetScopeAsync()' will recreate a new shell context.
                    _shellContexts.TryRemove(settings.Name, out _);
                }
            }

            return scope;
        }

        public async Task UpdateShellSettingsAsync(ShellSettings settings)
        {
            settings.VersionId = IdGenerator.GenerateId();
            await _shellSettingsManager.SaveSettingsAsync(settings);
            await ReloadShellContextAsync(settings);
        }

        /// <summary>
        /// A feature is enabled / disabled, the tenant needs to be released so that a new shell will be built.
        /// </summary>
        public Task ChangedAsync(ShellDescriptor descriptor, ShellSettings settings)
            => ReleaseShellContextAsync(settings);

        /// <summary>
        /// Reloads the settings and releases the shell so that a new one will be
        /// built for subsequent requests, while existing requests get flushed.
        /// </summary>
        /// <param name="settings">The <see cref="ShellSettings"/> to reload.</param>
        /// <param name="eventSource">
        /// Whether the related <see cref="ShellEvent"/> is invoked.
        /// </param>
        public async Task ReloadShellContextAsync(ShellSettings settings, bool eventSource = true)
        {
            if (ReloadingAsync != null && eventSource && settings.State != TenantState.Initializing)
            {
                foreach (var d in ReloadingAsync.GetInvocationList())
                {
                    await ((ShellEvent)d)(settings.Name);
                }
            }

            // A disabled shell still in use will be released by its last scope.
            if (!CanReleaseShell(settings))
            {
                _runningShellTable.Remove(settings);
                return;
            }

            if (settings.State != TenantState.Initializing)
            {
                settings = await _shellSettingsManager.LoadSettingsAsync(settings.Name);
            }

            var count = 0;
            while (count < ReloadShellMaxRetriesCount)
            {
                count++;

                if (_shellContexts.TryRemove(settings.Name, out var context))
                {
                    _runningShellTable.Remove(settings);
                    context.Release();
                }

                // Add a 'PlaceHolder' allowing to retrieve the settings until the shell will be rebuilt.
                if (!_shellContexts.TryAdd(settings.Name, new ShellContext.PlaceHolder { Settings = settings }))
                {
                    // Atomicity: We may have been the last to load the settings but unable to add the shell.
                    continue;
                }

                _shellSettings[settings.Name] = settings;

                if (CanRegisterShell(settings))
                {
                    _runningShellTable.Add(settings);
                }

                if (settings.State == TenantState.Initializing)
                {
                    return;
                }

                var currentVersionId = settings.VersionId;

                settings = await _shellSettingsManager.LoadSettingsAsync(settings.Name);

                // Consistency: We may have been the last to add the shell but not with the last settings.
                if (settings.VersionId == currentVersionId)
                {
                    return;
                }
            }

            throw new ShellHostReloadException(
                $"Unable to reload the tenant '{settings.Name}' as too many concurrent processes are trying to do so.");
        }

        /// <summary>
        /// Releases a shell so that a new one will be built for subsequent requests.
        /// Note: Can be used to free up resources after a given time of inactivity.
        /// </summary>
        /// <param name="settings">The <see cref="ShellSettings"/> to reload.</param>
        /// <param name="eventSource">
        /// Whether the related <see cref="ShellEvent"/> is invoked.
        /// </param>
        public async Task ReleaseShellContextAsync(ShellSettings settings, bool eventSource = true)
        {
            if (ReleasingAsync != null && eventSource && settings.State != TenantState.Initializing)
            {
                foreach (var d in ReleasingAsync.GetInvocationList())
                {
                    await ((ShellEvent)d)(settings.Name);
                }
            }

            // A disabled shell still in use will be released by its last scope.
            if (!CanReleaseShell(settings))
            {
                return;
            }

            if (_shellContexts.TryRemove(settings.Name, out var context))
            {
                context.Release();
            }

            // Add a 'PlaceHolder' allowing to retrieve the settings until the shell will be rebuilt.
            if (_shellContexts.TryAdd(settings.Name, new ShellContext.PlaceHolder { Settings = settings }))
            {
                _shellSettings[settings.Name] = settings;
            }

            return;
        }

        public IEnumerable<ShellContext> ListShellContexts() => _shellContexts.Values.ToArray();

        /// <summary>
        /// Tries to retrieve the shell context associated with the specified tenant.
        /// The shell may have been temporarily removed while releasing or reloading.
        /// </summary>
        public bool TryGetShellContext(string name, out ShellContext shellContext) => _shellContexts.TryGetValue(name, out shellContext);

        /// <summary>
        /// Tries to retrieve the shell settings associated with the specified tenant.
        /// </summary>
        public bool TryGetSettings(string name, out ShellSettings settings) => _shellSettings.TryGetValue(name, out settings);

        /// <summary>
        /// Retrieves all shell settings.
        /// </summary>
        public IEnumerable<ShellSettings> GetAllSettings() => _shellSettings.Values.ToArray();

        private async Task PreCreateAndRegisterShellsAsync()
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Start creation of shells");
            }

            // Load all extensions and features so that the controllers are registered in
            // 'ITypeFeatureProvider' and their areas defined in the application conventions.
            await _extensionManager.LoadFeaturesAsync();

            if (LoadingAsync != null)
            {
                foreach (var d in LoadingAsync.GetInvocationList())
                {
                    await ((ShellsEvent)d)();
                }
            }

            // Is there any tenant right now?
            var allSettings = (await _shellSettingsManager.LoadSettingsAsync()).Where(CanCreateShell).ToArray();
            var defaultSettings = allSettings.FirstOrDefault(s => s.Name == ShellHelper.DefaultShellName);

            // The 'Default' tenant is not running, run the Setup.
            if (defaultSettings?.State != TenantState.Running)
            {
                var setupContext = await CreateSetupContextAsync(defaultSettings);
                AddAndRegisterShell(setupContext);
            }

            // Pre-create and register all tenant shells.
            foreach (var settings in allSettings)
            {
                AddAndRegisterShell(new ShellContext.PlaceHolder { Settings = settings });
            };

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Done pre-creating and registering shells");
            }
        }

        /// <summary>
        /// Creates a shell context based on shell settings
        /// </summary>
        private Task<ShellContext> CreateShellContextAsync(ShellSettings settings)
        {
            if (settings.State == TenantState.Uninitialized)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Creating shell context for tenant '{TenantName}' setup", settings.Name);
                }

                return _shellContextFactory.CreateSetupContextAsync(settings);
            }
            else if (settings.State == TenantState.Disabled)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Creating disabled shell context for tenant '{TenantName}'", settings.Name);
                }

                return Task.FromResult(new ShellContext { Settings = settings });
            }
            else if (settings.State == TenantState.Running || settings.State == TenantState.Initializing)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Creating shell context for tenant '{TenantName}'", settings.Name);
                }

                return _shellContextFactory.CreateShellContextAsync(settings);
            }
            else
            {
                throw new InvalidOperationException("Unexpected shell state for " + settings.Name);
            }
        }

        /// <summary>
        /// Creates a transient shell for the default tenant's setup.
        /// </summary>
        private Task<ShellContext> CreateSetupContextAsync(ShellSettings defaultSettings)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Creating shell context for root setup.");
            }

            if (defaultSettings == null)
            {
                // Creates a default shell settings based on the configuration.
                var shellSettings = _shellSettingsManager.CreateDefaultSettings();
                shellSettings.Name = ShellHelper.DefaultShellName;
                shellSettings.State = TenantState.Uninitialized;
                defaultSettings = shellSettings;
            }

            return _shellContextFactory.CreateSetupContextAsync(defaultSettings);
        }

        /// <summary>
        /// Adds the shell and registers its settings in RunningShellTable
        /// </summary>
        private void AddAndRegisterShell(ShellContext context)
        {
            if (_shellContexts.TryAdd(context.Settings.Name, context))
            {
                _shellSettings[context.Settings.Name] = context.Settings;

                if (CanRegisterShell(context))
                {
                    RegisterShellSettings(context.Settings);
                }
            }
        }

        /// <summary>
        /// Whether or not a shell can be activated and added to the running shells.
        /// </summary>
        private bool CanRegisterShell(ShellContext context)
        {
            if (!CanRegisterShell(context.Settings))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Skipping shell context registration for tenant '{TenantName}'", context.Settings.Name);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers the shell settings in RunningShellTable
        /// </summary>
        private void RegisterShellSettings(ShellSettings settings)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Registering shell context for tenant '{TenantName}'", settings.Name);
            }

            _runningShellTable.Add(settings);
        }

        /// <summary>
        /// Whether or not a shell can be added to the list of available shells.
        /// </summary>
        private bool CanCreateShell(ShellSettings shellSettings)
        {
            return
                shellSettings.State == TenantState.Running ||
                shellSettings.State == TenantState.Uninitialized ||
                shellSettings.State == TenantState.Initializing ||
                shellSettings.State == TenantState.Disabled;
        }

        /// <summary>
        /// Whether or not a shell can be activated and added to the running shells.
        /// </summary>
        private bool CanRegisterShell(ShellSettings shellSettings)
        {
            return
                shellSettings.State == TenantState.Running ||
                shellSettings.State == TenantState.Uninitialized ||
                shellSettings.State == TenantState.Initializing;
        }

        /// <summary>
        /// Whether or not a shell can be released and removed from the list, false if disabled and still in use.
        /// Note: A disabled shell still in use will be released by its last scope, and keeping it in the list
        /// prevents a consumer from creating a new one that would have a null service provider.
        /// </summary>
        private bool CanReleaseShell(ShellSettings settings)
        {
            return settings.State != TenantState.Disabled || _shellContexts.TryGetValue(settings.Name, out var value) && value.ActiveScopes == 0;
        }

        public async Task RemoveShellAsync(ShellSettings settings)
        {
            if (_shellContexts.TryRemove(settings.Name, out var context))
            {
                _runningShellTable.Remove(settings);
                _shellSettings.Remove(settings.Name, out settings);
                context.Release();
            }

            using (var serviceScope = (await CreateShellContextAsync(settings)).CreateScope())
            {
                var shellEventHandlers = serviceScope.ServiceProvider.GetServices<IShellEventHandler>();
                await shellEventHandlers.InvokeAsync(x => x.Removing(settings), _logger);
            }

            await _shellSettingsManager.DeleteSettingsAsync(settings);

            var tenantFolder = Path.Combine(_shellOptions.Value.ShellsApplicationDataPath, _shellOptions.Value.ShellsContainerName, settings.Name);

            Directory.Delete(tenantFolder, true);
        }

        public void Dispose()
        {
            foreach (var shell in ListShellContexts())
            {
                shell.Dispose();
            }
        }
    }
}
