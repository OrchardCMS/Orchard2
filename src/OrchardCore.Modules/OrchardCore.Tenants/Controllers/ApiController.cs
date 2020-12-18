using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.Data;
using OrchardCore.Email;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using OrchardCore.Setup.Services;
using OrchardCore.Tenants.Events;
using OrchardCore.Tenants.Services;
using OrchardCore.Tenants.Utilities;
using OrchardCore.Tenants.ViewModels;

namespace OrchardCore.Tenants.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    public class ApiController : Controller
    {
        private readonly IShellHost _shellHost;
        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IEnumerable<DatabaseProvider> _databaseProviders;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISetupService _setupService;
        private readonly ShellSettings _currentShellSettings;
        private readonly IEmailAddressValidator _emailAddressValidator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IStringLocalizer S;

        public ApiController(
            IShellHost shellHost,
            ShellSettings currentShellSettings,
            IAuthorizationService authorizationService,
            IShellSettingsManager shellSettingsManager,
            IEnumerable<DatabaseProvider> databaseProviders,
            ISetupService setupService,
            IEmailAddressValidator emailAddressValidator,
            ILogger<ApiController> logger,
            IServiceProvider serviceProvider,
            IStringLocalizer<AdminController> stringLocalizer
        )
        {
            _setupService = setupService;
            _shellHost = shellHost;
            _authorizationService = authorizationService;
            _shellSettingsManager = shellSettingsManager;
            _databaseProviders = databaseProviders;
            _currentShellSettings = currentShellSettings;
            _emailAddressValidator = emailAddressValidator ?? throw new ArgumentNullException(nameof(emailAddressValidator));
            _serviceProvider = serviceProvider;
            S = stringLocalizer;
            _logger = logger;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(CreateApiViewModel model)
        {
            if (!IsDefaultShell())
            {
                return Forbid();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTenants))
            {
                return this.ChallengeOrForbid("Api");
            }

            if (!String.IsNullOrEmpty(model.Name) && !Regex.IsMatch(model.Name, @"^\w+$"))
            {
                ModelState.AddModelError(nameof(CreateApiViewModel.Name), S["Invalid tenant name. Must contain characters only and no spaces."]);
            }

            // Creates a default shell settings based on the configuration.
            var shellSettings = _shellSettingsManager.CreateDefaultSettings();

            shellSettings.Name = model.Name;
            shellSettings.RequestUrlHost = model.RequestUrlHost;
            shellSettings.RequestUrlPrefix = model.RequestUrlPrefix;
            shellSettings.State = TenantState.Uninitialized;

            shellSettings["ConnectionString"] = model.ConnectionString;
            shellSettings["TablePrefix"] = model.TablePrefix;
            shellSettings["DatabaseProvider"] = model.DatabaseProvider;
            shellSettings["Secret"] = Guid.NewGuid().ToString();
            shellSettings["RecipeName"] = model.RecipeName;

            if (String.IsNullOrWhiteSpace(shellSettings.RequestUrlHost) && String.IsNullOrWhiteSpace(shellSettings.RequestUrlPrefix))
            {
                ModelState.AddModelError(nameof(CreateApiViewModel.RequestUrlPrefix), S["Host and url prefix can not be empty at the same time."]);
            }

            if (!String.IsNullOrWhiteSpace(shellSettings.RequestUrlPrefix))
            {
                if (shellSettings.RequestUrlPrefix.Contains('/'))
                {
                    ModelState.AddModelError(nameof(CreateApiViewModel.RequestUrlPrefix), S["The url prefix can not contain more than one segment."]);
                }
            }

            if (ModelState.IsValid)
            {
                if (_shellHost.TryGetSettings(model.Name, out var settings))
                {
                    // Site already exists, return 201 for indempotency purpose

                    var token = _setupService.CreateSetupToken(settings);

                    return StatusCode(201, TenantHelperExtensions.GetEncodedUrl(settings, Request, token));
                }
                else
                {
                    await _shellHost.UpdateShellSettingsAsync(shellSettings);
                    var token = _setupService.CreateSetupToken(shellSettings);
                    var encodedUrl = TenantHelperExtensions.GetEncodedUrl(shellSettings, Request, token);

                    // Invoke modules to react to the tenant created event
                    var context = new TenantContext
                    {
                        ShellSettings = shellSettings,
                        EncodedUrl = encodedUrl
                    };

                    var tenantCreatedEventHandlers = _serviceProvider.GetRequiredService<IEnumerable<ITenantCreatedEventHandler>>();
                    if(tenantCreatedEventHandlers.Any())
                    {
                        await tenantCreatedEventHandlers.InvokeAsync((handler, conext) => handler.TenantCreated(context), context, _logger);
                    }

                    return Ok(encodedUrl);
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPost]
        [Route("setup")]
        public async Task<ActionResult> Setup(SetupApiViewModel model)
        {
            if (!IsDefaultShell())
            {
                return this.ChallengeOrForbid("Api");
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTenants))
            {
                return this.ChallengeOrForbid("Api");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!_emailAddressValidator.Validate(model.Email))
            {
                return BadRequest(S["Invalid email."]);
            }

            if (!_shellHost.TryGetSettings(model.Name, out var shellSettings))
            {
                ModelState.AddModelError(nameof(SetupApiViewModel.Name), S["Tenant not found: '{0}'", model.Name]);
            }

            if (shellSettings.State == TenantState.Running)
            {
                return StatusCode(201);
            }

            if (shellSettings.State != TenantState.Uninitialized)
            {
                return BadRequest(S["The tenant can't be setup."]);
            }

            var databaseProvider = shellSettings["DatabaseProvider"];

            if (String.IsNullOrEmpty(databaseProvider))
            {
                databaseProvider = model.DatabaseProvider;
            }

            var selectedProvider = _databaseProviders.FirstOrDefault(x => String.Equals(x.Value, databaseProvider, StringComparison.OrdinalIgnoreCase));

            if (selectedProvider == null)
            {
                return BadRequest(S["The database provider is not defined."]);
            }

            var tablePrefix = shellSettings["TablePrefix"];

            if (String.IsNullOrEmpty(tablePrefix))
            {
                tablePrefix = model.TablePrefix;
            }

            var connectionString = shellSettings["connectionString"];

            if (String.IsNullOrEmpty(connectionString))
            {
                connectionString = model.ConnectionString;
            }

            if (selectedProvider.HasConnectionString && String.IsNullOrEmpty(connectionString))
            {
                return BadRequest(S["The connection string is required for this database provider."]);
            }

            var recipeName = shellSettings["RecipeName"];

            if (String.IsNullOrEmpty(recipeName))
            {
                recipeName = model.RecipeName;
            }

            RecipeDescriptor recipeDescriptor = null;

            if (String.IsNullOrEmpty(recipeName))
            {
                if (model.Recipe == null)
                {
                    return BadRequest(S["Either 'Recipe' or 'RecipeName' is required."]);
                }

                var tempFilename = Path.GetTempFileName();

                using (var fs = System.IO.File.Create(tempFilename))
                {
                    await model.Recipe.CopyToAsync(fs);
                }

                var fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(tempFilename));

                recipeDescriptor = new RecipeDescriptor
                {
                    FileProvider = fileProvider,
                    BasePath = "",
                    RecipeFileInfo = fileProvider.GetFileInfo(Path.GetFileName(tempFilename))
                };
            }
            else
            {
                var setupRecipes = await _setupService.GetSetupRecipesAsync();
                recipeDescriptor = setupRecipes.FirstOrDefault(x => String.Equals(x.Name, recipeName, StringComparison.OrdinalIgnoreCase));

                if (recipeDescriptor == null)
                {
                    return BadRequest(S["Recipe '{0}' not found.", recipeName]);
                }
            }

            var setupContext = new SetupContext
            {
                ShellSettings = shellSettings,
                SiteName = model.SiteName,
                EnabledFeatures = null, // default list,
                AdminUsername = model.UserName,
                AdminEmail = model.Email,
                AdminPassword = model.Password,
                Errors = new Dictionary<string, string>(),
                Recipe = recipeDescriptor,
                SiteTimeZone = model.SiteTimeZone,
                DatabaseProvider = selectedProvider.Value,
                DatabaseConnectionString = connectionString,
                DatabaseTablePrefix = tablePrefix
            };

            var executionId = await _setupService.SetupAsync(setupContext);

            // Check if a component in the Setup failed
            if (setupContext.Errors.Any())
            {
                foreach (var error in setupContext.Errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }

                return StatusCode(500, ModelState);
            }

            return Ok(executionId);
        }

        private bool IsDefaultShell()
        {
            return String.Equals(_currentShellSettings.Name, ShellHelper.DefaultShellName, StringComparison.OrdinalIgnoreCase);
        }

        private string GetEncodedUrl(ShellSettings shellSettings, string token)
        {
            var requestHost = Request.Host;
            var host = shellSettings.RequestUrlHost?.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? requestHost.Host;

            var port = requestHost.Port;

            if (port.HasValue)
            {
                host += ":" + port;
            }

            var hostString = new HostString(host);

            var pathString = HttpContext.Features.Get<ShellContextFeature>().OriginalPathBase;

            if (!String.IsNullOrEmpty(shellSettings.RequestUrlPrefix))
            {
                pathString = pathString.Add('/' + shellSettings.RequestUrlPrefix);
            }

            QueryString queryString;

            if (!String.IsNullOrEmpty(token))
            {
                queryString = QueryString.Create("token", token);
            }

            return $"{Request.Scheme}://{hostString + pathString + queryString}";
        }
    }
}
