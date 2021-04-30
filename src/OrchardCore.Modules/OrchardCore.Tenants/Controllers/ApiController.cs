using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Abstractions.Setup;
using OrchardCore.Data;
using OrchardCore.Email;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using OrchardCore.Setup.Core;
using OrchardCore.Setup.Services;
using OrchardCore.Tenants.ViewModels;

namespace OrchardCore.Tenants.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    public class ApiController : Controller
    {
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _currentShellSettings;
        private readonly IAuthorizationService _authorizationService;
        private readonly IShellSettingsManager _shellSettingsManager;
        private readonly IDataProtectionProvider _dataProtectorProvider;
        private readonly IUrlService _urlService;
        private readonly ISetupService _setupService;
        private readonly IClock _clock;
        private readonly IEmailAddressValidator _emailAddressValidator;
        private readonly IdentityOptions _identityOptions;
        private readonly IEnumerable<DatabaseProvider> _databaseProviders;
        private readonly IStringLocalizer S;

        public ApiController(
            IShellHost shellHost,
            ShellSettings currentShellSettings,
            IAuthorizationService authorizationService,
            IShellSettingsManager shellSettingsManager,
            IDataProtectionProvider dataProtectorProvider,
            IUrlService urlService,
            ISetupService setupService,
            IClock clock,
            IEmailAddressValidator emailAddressValidator,
            IOptions<IdentityOptions> identityOptions,
            IEnumerable<DatabaseProvider> databaseProviders,
            IStringLocalizer<AdminController> stringLocalizer)
        {
            _shellHost = shellHost;
            _currentShellSettings = currentShellSettings;
            _authorizationService = authorizationService;
            _dataProtectorProvider = dataProtectorProvider;
            _shellSettingsManager = shellSettingsManager;
            _urlService = urlService;
            _setupService = setupService;
            _clock = clock;
            _emailAddressValidator = emailAddressValidator;
            _identityOptions = identityOptions.Value;
            _databaseProviders = databaseProviders;
            S = stringLocalizer;
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

                    var token = CreateSetupToken(settings);
                    var tokenParams = new Dictionary<string, string>()
                    {
                         {"token", token },
                    };
                    var url = _urlService.GetEncodedUrl(settings, tokenParams);

                    return StatusCode(201, url);
                }
                else
                {
                    await _shellHost.UpdateShellSettingsAsync(shellSettings);

                    var token = CreateSetupToken(shellSettings);
                    var tokenParams = new Dictionary<string, string>()
                    {
                         {"token",token },
                    };
                    var url = _urlService.GetEncodedUrl(shellSettings, tokenParams);

                    return Ok(url);
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

            if (!String.IsNullOrEmpty(model.UserName) && model.UserName.Any(c => !_identityOptions.User.AllowedUserNameCharacters.Contains(c)))
            {
                ModelState.AddModelError(nameof(model.UserName), S["User name '{0}' is invalid, can only contain letters or digits.", model.UserName]);
            }

            // Only add additional error if attribute validation has passed.
            if (!String.IsNullOrEmpty(model.Email) && !_emailAddressValidator.Validate(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), S["The email is invalid."]);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
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
                EnabledFeatures = null, // default list,
                Errors = new Dictionary<string, string>(),
                Recipe = recipeDescriptor,
                Properties = new Dictionary<string, object>
                {
                    { SetupConstants.SiteName, model.SiteName },
                    { SetupConstants.AdminUsername, model.UserName },
                    { SetupConstants.AdminEmail, model.Email },
                    { SetupConstants.AdminPassword, model.Password },
                    { SetupConstants.SiteTimeZone, model.SiteTimeZone },
                    { SetupConstants.DatabaseProvider, selectedProvider.Value },
                    { SetupConstants.DatabaseConnectionString, connectionString },
                    { SetupConstants.DatabaseTablePrefix, tablePrefix },
                }
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

        private string CreateSetupToken(ShellSettings shellSettings)
        {
            // Create a public url to setup the new tenant
            var dataProtector = _dataProtectorProvider.CreateProtector("Tokens").ToTimeLimitedDataProtector();
            var token = dataProtector.Protect(shellSettings["Secret"], _clock.UtcNow.Add(new TimeSpan(24, 0, 0)));
            return token;
        }
    }
}
