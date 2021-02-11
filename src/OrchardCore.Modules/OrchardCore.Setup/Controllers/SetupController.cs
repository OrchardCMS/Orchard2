using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Data;
using OrchardCore.Email;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.Recipes.Models;
using OrchardCore.Setup.Services;
using OrchardCore.Setup.ViewModels;

namespace OrchardCore.Setup.Controllers
{
    public class SetupController : Controller
    {
        private readonly IClock _clock;
        private readonly ISetupService _setupService;
        private readonly ShellSettings _shellSettings;
        private readonly IShellHost _shellHost;
        private IdentityOptions _identityOptions;
        private readonly IEmailAddressValidator _emailAddressValidator;
        private readonly IEnumerable<DatabaseProvider> _databaseProviders;
        private readonly ILogger _logger;
        private readonly IStringLocalizer S;

        public SetupController(
            IClock clock,
            ISetupService setupService,
            ShellSettings shellSettings,
            IShellHost shellHost,
            IOptions<IdentityOptions> identityOptions,
            IEmailAddressValidator emailAddressValidator,
            IEnumerable<DatabaseProvider> databaseProviders,
            IStringLocalizer<SetupController> localizer,
            ILogger<SetupController> logger)
        {
            _clock = clock;
            _setupService = setupService;
            _shellSettings = shellSettings;
            _shellHost = shellHost;
            _identityOptions = identityOptions.Value;
            _emailAddressValidator = emailAddressValidator;
            _databaseProviders = databaseProviders;
            _logger = logger;
            S = localizer;
        }

        public async Task<ActionResult> Index(string token)
        {
            var recipes = await _setupService.GetSetupRecipesAsync();
            var defaultRecipe = recipes.FirstOrDefault(x => x.Tags.Contains("default")) ?? recipes.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(_shellSettings["Secret"]))
            {
                if (string.IsNullOrEmpty(token) || !await IsTokenValid(token))
                {
                    _logger.LogWarning("An attempt to access '{TenantName}' without providing a secret was made", _shellSettings.Name);
                    return StatusCode(404);
                }
            }

            var model = new SetupViewModel
            {
                DatabaseProviders = _databaseProviders,
                Recipes = recipes,
                RecipeName = defaultRecipe?.Name,
                Secret = token
            };

            CopyShellSettingsValues(model);

            if (!String.IsNullOrEmpty(_shellSettings["TablePrefix"]))
            {
                model.DatabaseConfigurationPreset = true;
                model.TablePrefix = _shellSettings["TablePrefix"];
            }

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        public async Task<ActionResult> IndexPOST(SetupViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(_shellSettings["Secret"]))
            {
                if (string.IsNullOrEmpty(model.Secret) || !await IsTokenValid(model.Secret))
                {
                    _logger.LogWarning("An attempt to access '{TenantName}' without providing a valid secret was made", _shellSettings.Name);
                    return StatusCode(404);
                }
            }

            model.DatabaseProviders = _databaseProviders;
            model.Recipes = await _setupService.GetSetupRecipesAsync();

            var selectedProvider = model.DatabaseProviders.FirstOrDefault(x => x.Value == model.DatabaseProvider);

            if (!model.DatabaseConfigurationPreset)
            {
                if (selectedProvider != null && selectedProvider.HasConnectionString && String.IsNullOrWhiteSpace(model.ConnectionString))
                {
                    ModelState.AddModelError(nameof(model.ConnectionString), S["The connection string is mandatory for this provider."]);
                }
            }

            if (String.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), S["The password is required."]);
            }

            if (model.Password != model.PasswordConfirmation)
            {
                ModelState.AddModelError(nameof(model.PasswordConfirmation), S["The password confirmation doesn't match the password."]);
            }

            RecipeDescriptor selectedRecipe = null;
            if (!string.IsNullOrEmpty(_shellSettings["RecipeName"]))
            {
                selectedRecipe = model.Recipes.FirstOrDefault(x => x.Name == _shellSettings["RecipeName"]);
                if (selectedRecipe == null)
                {
                    ModelState.AddModelError(nameof(model.RecipeName), S["Invalid recipe."]);
                }
            }
            else if (String.IsNullOrEmpty(model.RecipeName) || (selectedRecipe = model.Recipes.FirstOrDefault(x => x.Name == model.RecipeName)) == null)
            {
                ModelState.AddModelError(nameof(model.RecipeName), S["Invalid recipe."]);
            }

            // Only add additional errors if attribute validation has passed.
            if (!String.IsNullOrEmpty(model.Email) && !_emailAddressValidator.Validate(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), S["The email is invalid."]);
            }

            if (!String.IsNullOrEmpty(model.UserName) && model.UserName.Any(c => !_identityOptions.User.AllowedUserNameCharacters.Contains(c)))
            {
                ModelState.AddModelError(nameof(model.UserName), S["User name '{0}' is invalid, can only contain letters or digits.", model.UserName]);
            }

            if (!ModelState.IsValid)
            {
                CopyShellSettingsValues(model);
                return View(model);
            }

            var setupContext = new SetupContext
            {
                ShellSettings = _shellSettings,
                SiteName = model.SiteName,
                EnabledFeatures = null, // default list,
                AdminUsername = model.UserName,
                AdminEmail = model.Email,
                AdminPassword = model.Password,
                Errors = new Dictionary<string, string>(),
                Recipe = selectedRecipe,
                SiteTimeZone = model.SiteTimeZone
            };

            if (!string.IsNullOrEmpty(_shellSettings["ConnectionString"]))
            {
                setupContext.DatabaseProvider = _shellSettings["DatabaseProvider"];
                setupContext.DatabaseConnectionString = _shellSettings["ConnectionString"];
                setupContext.DatabaseTablePrefix = _shellSettings["TablePrefix"];
            }
            else
            {
                setupContext.DatabaseProvider = model.DatabaseProvider;

                if (model.DatabaseProvider == "Sqlite")
                {
                    const string defaultDatabaseName = "orchardcore.db";
                    model.ConnectionString = $"Data Source={defaultDatabaseName};Cache=Shared";
                }

                setupContext.DatabaseConnectionString = model.ConnectionString;
                setupContext.DatabaseTablePrefix = model.TablePrefix;
            }

            var executionId = await _setupService.SetupAsync(setupContext);

            // Check if a component in the Setup failed
            if (setupContext.Errors.Any())
            {
                foreach (var error in setupContext.Errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }

                return View(model);
            }

            return Redirect("~/");
        }

        private void CopyShellSettingsValues(SetupViewModel model)
        {
            if (!String.IsNullOrEmpty(_shellSettings["ConnectionString"]))
            {
                model.DatabaseConfigurationPreset = true;
                model.ConnectionString = _shellSettings["ConnectionString"];
            }

            if (!String.IsNullOrEmpty(_shellSettings["RecipeName"]))
            {
                model.RecipeNamePreset = true;
                model.RecipeName = _shellSettings["RecipeName"];
            }

            if (!String.IsNullOrEmpty(_shellSettings["DatabaseProvider"]))
            {
                model.DatabaseConfigurationPreset = true;
                model.DatabaseProvider = _shellSettings["DatabaseProvider"];
            }
            else
            {
                model.DatabaseProvider = model.DatabaseProviders.FirstOrDefault(p => p.IsDefault)?.Value;
            }

            if (!String.IsNullOrEmpty(_shellSettings["Description"]))
            {
                model.Description = _shellSettings["Description"];
            }
        }

        private async Task<bool> IsTokenValid(string token)
        {
            try
            {
                var result = false;

                var shellScope = await _shellHost.GetScopeAsync(ShellHelper.DefaultShellName);

                await shellScope.UsingAsync(scope =>
                {
                    var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
                    var dataProtector = dataProtectionProvider.CreateProtector("Tokens").ToTimeLimitedDataProtector();

                    var tokenValue = dataProtector.Unprotect(token, out var expiration);

                    if (_clock.UtcNow < expiration.ToUniversalTime())
                    {
                        if (_shellSettings["Secret"] == tokenValue)
                        {
                            result = true;
                        }
                    }

                    return Task.CompletedTask;
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in decrypting the token");
            }

            return false;
        }
    }
}
