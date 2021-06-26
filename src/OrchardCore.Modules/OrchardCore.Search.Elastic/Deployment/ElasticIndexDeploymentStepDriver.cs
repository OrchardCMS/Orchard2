using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Search.Elastic.ViewModels;

namespace OrchardCore.Search.Elastic.Deployment
{
    public class ElasticIndexDeploymentStepDriver : DisplayDriver<DeploymentStep, ElasticIndexDeploymentStep>
    {
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;

        public ElasticIndexDeploymentStepDriver(ElasticIndexSettingsService elasticIndexSettingsService)
        {
            _elasticIndexSettingsService = elasticIndexSettingsService;
        }

        public override IDisplayResult Display(ElasticIndexDeploymentStep step)
        {
            return
                Combine(
                    View("ElasticIndexDeploymentStep_Fields_Summary", step).Location("Summary", "Content"),
                    View("ElasticIndexDeploymentStep_Fields_Thumbnail", step).Location("Thumbnail", "Content")
                );
        }

        public override IDisplayResult Edit(ElasticIndexDeploymentStep step)
        {
            return Initialize<ElasticIndexDeploymentStepViewModel>("ElasticIndexDeploymentStep_Fields_Edit", async model =>
            {
                model.IncludeAll = step.IncludeAll;
                model.IndexNames = step.IndexNames;
                model.AllIndexNames = (await _elasticIndexSettingsService.GetSettingsAsync()).Select(x => x.IndexName).ToArray();
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(ElasticIndexDeploymentStep step, IUpdateModel updater)
        {
            step.IndexNames = Array.Empty<string>();

            await updater.TryUpdateModelAsync(step,
                                              Prefix,
                                              x => x.IndexNames,
                                              x => x.IncludeAll);

            // don't have the selected option if include all
            if (step.IncludeAll)
            {
                step.IndexNames = Array.Empty<string>();
            }

            return Edit(step);
        }
    }
}
