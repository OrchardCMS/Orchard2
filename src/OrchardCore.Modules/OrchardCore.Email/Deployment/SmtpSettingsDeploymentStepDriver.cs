using System.Threading.Tasks;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Email.ViewModels;

namespace OrchardCore.Email.Deployment
{
    public class SmtpSettingsDeploymentStepDriver : DisplayDriver<DeploymentStep, SmtpSettingsDeploymentStep>
    {
        public override IDisplayResult Display(SmtpSettingsDeploymentStep step)
        {
            return
                Combine(
                    View("SmtpSettingsDeploymentStep_Fields_Summary", step).Location("Summary", "Content"),
                    View("SmtpSettingsDeploymentStep_Fields_Thumbnail", step).Location("Thumbnail", "Content")
                );
        }
        public override IDisplayResult Edit(SmtpSettingsDeploymentStep step)
        {
            return Initialize<SmtpSettingsDeploymentStepViewModel>("SmtpSettingsDeploymentStep_Fields_Edit", model =>
            {
                model.Password = step.Password;
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(SmtpSettingsDeploymentStep step, IUpdateModel updater)
        {
            await updater.TryUpdateModelAsync(step, Prefix, x => x.Password);

            return Edit(step);
        }
    }
}
