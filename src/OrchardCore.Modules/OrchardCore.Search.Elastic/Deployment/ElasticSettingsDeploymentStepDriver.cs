using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.Search.Elastic.Deployment
{
    public class ElasticSettingsDeploymentStepDriver : DisplayDriver<DeploymentStep, ElasticSettingsDeploymentStep>
    {
        public override IDisplayResult Display(ElasticSettingsDeploymentStep step)
        {
            return
                Combine(
                    View("ElasticSettingsDeploymentStep_Fields_Summary", step).Location("Summary", "Content"),
                    View("ElasticSettingsDeploymentStep_Fields_Thumbnail", step).Location("Thumbnail", "Content")
                );
        }

        public override IDisplayResult Edit(ElasticSettingsDeploymentStep step)
        {
            return View("ElasticSettingsDeploymentStep_Fields_Edit", step).Location("Content");
        }
    }
}
