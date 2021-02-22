using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using OrchardCore.Recipes.Services;
using OrchardCore.Recipes.ViewModels;
using OrchardCore.Security;
using OrchardCore.Settings;

namespace OrchardCore.Recipes.Controllers
{
    public class AdminController : Controller
    {
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly IExtensionManager _extensionManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEnumerable<IRecipeHarvester> _recipeHarvesters;
        private readonly IRecipeExecutor _recipeExecutor;
        private readonly IEnumerable<IRecipeEnvironmentProvider> _environmentProviders;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer H;
        private readonly ILogger _logger;

        public AdminController(
            IShellHost shellHost,
            ShellSettings shellSettings,
            IExtensionManager extensionManager,
            IAuthorizationService authorizationService,
            IEnumerable<IRecipeHarvester> recipeHarvesters,
            IRecipeExecutor recipeExecutor,
            IEnumerable<IRecipeEnvironmentProvider> environmentProviders,
            INotifier notifier,
            IHtmlLocalizer<AdminController> localizer,
            ILogger<AdminController> logger)
        {
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _extensionManager = extensionManager;
            _authorizationService = authorizationService;
            _recipeHarvesters = recipeHarvesters;
            _recipeExecutor = recipeExecutor;
            _environmentProviders = environmentProviders;
            _notifier = notifier;
            H = localizer;
            _logger = logger;
        }

        public async Task<ActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, StandardPermissions.SiteOwner))
            {
                return Forbid();
            }

            var recipeCollections = await Task.WhenAll(_recipeHarvesters.Select(x => x.HarvestRecipesAsync()));
            var recipes = recipeCollections.SelectMany(x => x);

            // Do not display the setup recipes and the ones whith the hidden tag
            recipes = recipes.Where(r => r.IsSetupRecipe == false && !r.Tags.Contains("hidden", StringComparer.InvariantCultureIgnoreCase));

            var features = _extensionManager.GetFeatures();

            var model = recipes.Select(recipe => new RecipeViewModel
            {
                Name = recipe.Name,
                DisplayName = recipe.DisplayName,
                FileName = recipe.RecipeFileInfo.Name,
                BasePath = recipe.BasePath,
                Tags = recipe.Tags,
                IsSetupRecipe = recipe.IsSetupRecipe,
                Feature = features.FirstOrDefault(f => recipe.BasePath.Contains(f.Extension.SubPath))?.Name ?? "Application",
                Description = recipe.Description
            }).ToArray();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Execute(string basePath, string fileName)
        {
            if (!await _authorizationService.AuthorizeAsync(User, StandardPermissions.SiteOwner))
            {
                return Forbid();
            }

            var recipeCollections = await Task.WhenAll(_recipeHarvesters.Select(x => x.HarvestRecipesAsync()));
            var recipes = recipeCollections.SelectMany(x => x);

            var recipe = recipes.FirstOrDefault(c => c.RecipeFileInfo.Name == fileName && c.BasePath == basePath);

            if (recipe == null)
            {
                _notifier.Error(H["Recipe was not found."]);
                return RedirectToAction("Index");
            }

            var environment = new Dictionary<string, object>();
            await _environmentProviders.InvokeAsync((provider, env) => provider.SetEnvironmentAsync(env), environment, _logger);

            var executionId = Guid.NewGuid().ToString("n");

            // Set shell state to "Initializing" so that subsequent HTTP requests
            // are responded to with "Service Unavailable" while running the recipe.
            _shellSettings.State = TenantState.Initializing;

            try
            {
                await _recipeExecutor.ExecuteAsync(executionId, recipe, environment, CancellationToken.None);
            }
            finally
            {
                // Don't lock the tenant if the recipe fails.
                _shellSettings.State = TenantState.Running;
            }

            await _shellHost.ReleaseShellContextAsync(_shellSettings);

            _notifier.Success(H["The recipe '{0}' has been run successfully.", recipe.DisplayName]);
            return RedirectToAction("Index");
        }
    }
}
