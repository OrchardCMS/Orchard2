using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.Features.Recipes.Executors
{
    /// <summary>
    /// This recipe step enables or disables a set of features.
    /// </summary>
    public class FeatureStep : IRecipeStepHandler
    {
        private readonly IExtensionManager _extensionManager;
        private IFeatureValidationService _featureValidationService;
        private readonly IShellFeaturesManager _shellFeatureManager;
        private readonly FeatureOptions _featureOptions;
        private readonly ShellSettings _shellSettings;

        public FeatureStep(
            IExtensionManager extensionManager,
            IFeatureValidationService featureValidationService,
            IShellFeaturesManager shellFeatureManager,
            IOptions<FeatureOptions> featureOptions,
            ShellSettings shellSettings)
        {
            _extensionManager = extensionManager;
            _featureValidationService = featureValidationService;
            _shellFeatureManager = shellFeatureManager;
            _featureOptions = featureOptions.Value;
            _shellSettings = shellSettings;
        }

        public Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "Feature", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var step = context.Step.ToObject<FeatureStepModel>();
            var features = _extensionManager.GetFeatures();

            var featuresToDisable = features.Where(x => step.Disable?.Contains(x.Id) == true).ToList();
            var featuresToEnable = features.Where(x => step.Enable?.Contains(x.Id) == true && FeatureIsAllowed(x)).ToList();

            if (featuresToDisable.Count > 0 || featuresToEnable.Count > 0)
            {
                return _shellFeatureManager.UpdateFeaturesAsync(featuresToDisable, featuresToEnable, true);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks whether the feature is allowed for the current tenant
        /// </summary>
        private bool FeatureIsAllowed(IFeatureInfo feature)
        {
            if (!_featureValidationService.IsFeatureValid(feature.Id))
            {
                return false;
            }

            // Checks if the feature is only allowed on the Default tenant
            return _shellSettings.Name == ShellHelper.DefaultShellName || !feature.DefaultTenantOnly;
        }

        private class FeatureStepModel
        {
            public string Name { get; set; }
            public string[] Disable { get; set; }
            public string[] Enable { get; set; }
        }
    }
}
