using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings.ViewModels;

namespace OrchardCore.Settings.Deployment
{
    public class SiteSettingsPropertyDeploymentStepDriver<TModel> : DisplayDriver<DeploymentStep, SiteSettingsPropertyDeploymentStep<TModel>> where TModel : class
    {
        private readonly string _title;
        private readonly string _description;

        public SiteSettingsPropertyDeploymentStepDriver(string title, string description)
        {
            _title = title;
            _description = description;
        }

        public override IDisplayResult Display(SiteSettingsPropertyDeploymentStep<TModel> step)
        {
            // TODO move this initialization to a BuildViewModel method.
            return
                Combine(
                    Initialize<SiteSettingsPropertyDeploymentStepViewModel>("SiteSettingsPropertyDeploymentStep_Fields_Summary", model =>
                    {
                        model.Title = _title;
                        model.Description = _description;
                    }).Location("Summary", "Content"),
                    Initialize<SiteSettingsPropertyDeploymentStepViewModel>("SiteSettingsPropertyDeploymentStep_Fields_Thumbnail", model =>
                    {
                        model.Title = _title;
                        model.Description = _description;
                    }).Location("Thumbnail", "Content")
                );
        }

        public override IDisplayResult Edit(SiteSettingsPropertyDeploymentStep<TModel> step)
        {
            return Initialize<SiteSettingsPropertyDeploymentStepViewModel>("SiteSettingsPropertyDeploymentStep_Fields_Edit", model =>
            {
                model.Title = _title;
                model.Description = _description;
            }).Location("Content");
        }
    }
}
