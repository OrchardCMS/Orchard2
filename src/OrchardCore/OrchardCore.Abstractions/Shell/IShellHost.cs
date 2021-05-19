using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Events;
using OrchardCore.Environment.Shell.Scope;

namespace OrchardCore.Environment.Shell
{
    public interface IShellHost : IShellEvents, IShellDescriptorManagerEventHandler
    {
        /// <summary>
        /// Ensure that all the <see cref="ShellContext"/> are pre-created and available to process requests.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Returns an existing <see cref="ShellContext"/> or creates a new one if necessary.
        /// </summary>
        Task<ShellContext> GetOrCreateShellContextAsync(ShellSettings settings);

        /// <summary>
        /// Creates a standalone service scope that can be used to resolve local services.
        /// </summary>
        Task<ShellScope> GetScopeAsync(ShellSettings settings);

        /// <summary>
        /// Updates an existing shell configuration and then reloads the shell.
        /// </summary>
        Task UpdateShellSettingsAsync(ShellSettings settings);

        /// <summary>
        /// Reloads the settings and releases the shell so that a new one will be
        /// built for subsequent requests, while existing requests get flushed.
        /// </summary>
        /// <param name="settings">The <see cref="ShellSettings"/> to reload.</param>
        /// <param name="eventSource">
        /// Whether the related <see cref="ShellEvent"/> is invoked.
        /// </param>
        Task ReloadShellContextAsync(ShellSettings settings, bool eventSource = true);

        /// <summary>
        /// Releases a shell so that a new one will be built for subsequent requests.
        /// Note: Can be used to free up resources after a given time of inactivity.
        /// </summary>
        /// <param name="settings">The <see cref="ShellSettings"/> to reload.</param>
        /// <param name="eventSource">
        /// Whether the related <see cref="ShellEvent"/> is invoked.
        /// </param>
        Task ReleaseShellContextAsync(ShellSettings settings, bool eventSource = true);

        /// <summary>
        /// Lists all available <see cref="ShellContext"/> instances.
        /// A shell might have been released or not yet built, if so 'shell.Released' is true and
        /// 'shell.CreateScope()' return null, but you can still use 'GetScopeAsync(shell.Settings)'.
        /// </summary>
        IEnumerable<ShellContext> ListShellContexts();

        /// <summary>
        /// Tries to retrieve the shell context associated with the specified tenant.
        /// The shell may have been temporarily removed while releasing or reloading.
        /// </summary>
        bool TryGetShellContext(string name, out ShellContext shellContext);

        /// <summary>
        /// Tries to retrieve the shell settings associated with the specified tenant.
        /// </summary>
        bool TryGetSettings(string name, out ShellSettings settings);

        /// <summary>
        /// Retrieves all shell settings.
        /// </summary>
        IEnumerable<ShellSettings> GetAllSettings();

        /// <summary>
        /// Removes an existing shell and destroys the tenant folder.
        /// </summary>
        /// <param name="settings">The <see cref="ShellSettings"/> object representing the shell to delete.</param>
        Task RemoveShellAsync(ShellSettings settings);
    }
}
