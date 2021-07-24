using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Contents.ViewModels;
using OrchardCore.Data.Migration;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;

namespace OrchardCore.Contents.Deployment.ExportContentToDeploymentTarget
{
    [Feature("OrchardCore.Contents.Deployment.ExportContentToDeploymentTarget")]
    public class ExportContentToDeploymentTargetStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<INavigationProvider, ExportContentToDeploymentTargetAdminMenu>();

            services.AddScoped<IDisplayDriver<ISite>, ExportContentToDeploymentTargetSettingsDisplayDriver>();

            services.AddTransient<IDeploymentSource, ExportContentToDeploymentTargetDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<ExportContentToDeploymentTargetDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, ExportContentToDeploymentTargetDeploymentStepDriver>();

            services.AddScoped<IDataMigration, ExportContentToDeploymentTargetMigrations>();
            services.AddScoped<IContentDisplayDriver, ExportContentToDeploymentTargetContentDriver>();
            services.AddScoped<IDisplayDriver<ContentOptionsViewModel>, ExportContentToDeploymentTargetContentsAdminListDisplayDriver>();

            services.AddSiteSettingsPropertyDeploymentStep<ExportContentToDeploymentTargetSettings, ExportContentToDeploymentTargetStartup>(S => S["Export Content To Deployment Target settings"], S => S["Exports the Export Content To Deployment Target settings."]);
        }
    }
}
