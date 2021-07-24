using System;
using System.Threading.Tasks;
using OrchardCore.Entities;
using OrchardCore.Google.Analytics.Settings;
using OrchardCore.Google.Analytics.ViewModels;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.Settings;

namespace OrchardCore.Google.Analytics.Recipes
{
    /// <summary>
    /// This recipe step sets Google Analytics settings.
    /// </summary>
    public class GoogleAnalyticsSettingsStep : IRecipeStepHandler
    {
        private readonly ISiteService _siteService;
        public GoogleAnalyticsSettingsStep(ISiteService siteService)
        {
            _siteService = siteService;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, nameof(GoogleAnalyticsSettings), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step.ToObject<GoogleAnalyticsSettingsViewModel>();
            var container = await _siteService.LoadSiteSettingsAsync();
            
            container.Alter<GoogleAnalyticsSettings>(nameof(GoogleAnalyticsSettings), aspect =>
            {
                aspect.SettingEntries = model.SettingEntries;
            });

            await _siteService.UpdateSiteSettingsAsync(container);
        }
    }
}
