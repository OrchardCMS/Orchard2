using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Data.Migration;
using OrchardCore.Deployment.Controllers;
using OrchardCore.Deployment.Core;
using OrchardCore.Deployment.Deployment;
using OrchardCore.Deployment.Indexes;
using OrchardCore.Deployment.Recipes;
using OrchardCore.Deployment.Services;
using OrchardCore.Deployment.Steps;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using YesSql.Indexes;

namespace OrchardCore.Deployment
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;

        public Startup(IOptions<AdminOptions> adminOptions)
        {
            _adminOptions = adminOptions.Value;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddDeploymentServices();

            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IPermissionProvider, Permissions>();

            services.AddScoped<IDisplayManager<DeploymentStep>, DisplayManager<DeploymentStep>>();
            services.AddSingleton<IDeploymentTargetProvider, FileDownloadDeploymentTargetProvider>();
            services.AddSingleton<IDeploymentTargetProvider, PreviewDeploymentTargetProvider>();

            // Custom File deployment step
            services.AddTransient<IDeploymentSource, CustomFileDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<CustomFileDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, CustomFileDeploymentStepDriver>();

            // Recipe File deployment step
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<RecipeFileDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, RecipeFileDeploymentStepDriver>();

            services.AddSingleton<IIndexProvider, DeploymentPlanIndexProvider>();
            services.AddTransient<IDataMigration, Migrations>();

            services.AddScoped<IDeploymentPlanService, DeploymentPlanService>();

            services.AddRecipeExecutionStep<DeploymentPlansRecipeStep>();

            services.AddTransient<IDeploymentSource, DeploymentPlanDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<DeploymentPlanDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, DeploymentPlanDeploymentStepDriver>();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var deploymentPlanControllerName = typeof(DeploymentPlanController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "DeploymentPlanIndex",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Index",
                defaults: new { controller = deploymentPlanControllerName, action = nameof(DeploymentPlanController.Index) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanCreate",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Create",
                defaults: new { controller = deploymentPlanControllerName, action = nameof(DeploymentPlanController.Create) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanDelete",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Delete/{id}",
                defaults: new { controller = deploymentPlanControllerName, action = nameof(DeploymentPlanController.Delete) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanDisplay",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Display/{id}",
                defaults: new { controller = deploymentPlanControllerName, action = nameof(DeploymentPlanController.Display) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanEdit",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Edit/{id}",
                defaults: new { controller = deploymentPlanControllerName, action = nameof(DeploymentPlanController.Edit) }
            );

            routes.MapAreaControllerRoute(
                name: "DeploymentPlanImport",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Import/Index",
                defaults: new { controller = typeof(ImportController).ControllerName(), action = nameof(ImportController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "DeploymentPlanImportJson",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/Import/Json",
                defaults: new { controller = typeof(ImportController).ControllerName(), action = nameof(ImportController.Json) }
            );

            routes.MapAreaControllerRoute(
                name: "DeploymentPlanExportFileExecute",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/ExportFile/Execute",
                defaults: new { controller = typeof(ExportFileController).ControllerName(), action = nameof(ExportFileController.Execute) }
            );

            // Steps
            var stepControllerName = typeof(StepController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "DeploymentPlanCreateStep",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/{id}/Step/Create",
                defaults: new { controller = stepControllerName, action = nameof(StepController.Create) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanDeleteStep",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/{id}/Step/{stepId}/Delete",
                defaults: new { controller = stepControllerName, action = nameof(StepController.Delete) }
            );
            routes.MapAreaControllerRoute(
                name: "DeploymentPlanEditStep",
                areaName: "OrchardCore.Deployment",
                pattern: _adminOptions.AdminUrlPrefix + "/DeploymentPlan/{id}/Step/{stepId}/Edit",
                defaults: new { controller = stepControllerName, action = nameof(StepController.Edit) }
            );
        }
    }
}
