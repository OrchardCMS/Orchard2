using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.DataLocalization.Deployment
{
    public class AllDataTranslationsDeploymentStepDriver : DisplayDriver<DeploymentStep, AllDataTranslationsDeploymentStep>
    {
        public override IDisplayResult Display(AllDataTranslationsDeploymentStep step)
        {
            return
                Combine(
                    View("AllDataTranslationsDeploymentStep_Summary", step).Location("Summary", "Content"),
                    View("AllDataTranslationsDeploymentStep_Thumbnail", step).Location("Thumbnail", "Content")
                );
        }

        public override IDisplayResult Edit(AllDataTranslationsDeploymentStep step)
        {
            return View("AllDataTranslationsDeploymentStep_Edit", step).Location("Content");
        }
    }
}
