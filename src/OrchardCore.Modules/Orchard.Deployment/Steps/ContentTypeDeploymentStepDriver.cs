using System;
using System.Threading.Tasks;
using Orchard.DisplayManagement.Handlers;
using Orchard.DisplayManagement.ModelBinding;
using Orchard.DisplayManagement.Views;

namespace Orchard.Deployment.Steps
{
    public class ContentTypeDeploymentStepDriver : DisplayDriver<DeploymentStep, ContentTypeDeploymentStep>
    {
        public override IDisplayResult Display(ContentTypeDeploymentStep step)
        {
            return 
                Combine(
                    Shape("ContentTypeDeploymentStep", step).Location("Summary", "Content"),
                    Shape("ContentTypeDeploymentStep_Thumbnail", step).Location("Thumbnail", "Content")
                );
        }

        public override IDisplayResult Edit(ContentTypeDeploymentStep step)
        {
            return Shape("ContentTypeDeploymentStep_Edit", step).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentTypeDeploymentStep step, IUpdateModel updater)
        {
            // Initializes the value to empty otherwise the model is not updated is no type is selected
            step.ContentTypes = Array.Empty<string>();

            await updater.TryUpdateModelAsync(step, Prefix, x => x.ContentTypes);

            return Edit(step);
        }
    }
}
