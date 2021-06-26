using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Search.Elastic.Controllers;
using OrchardCore.Search.Elastic.Deployment;
using OrchardCore.Search.Elastic.Drivers;
using OrchardCore.Search.Elastic.Handlers;
using OrchardCore.Search.Elastic.Recipes;
using OrchardCore.Search.Elastic.Services;
using OrchardCore.Search.Elastic.Settings;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Queries;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Environment.Shell.Configuration;
using Microsoft.Extensions.Logging;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Search.Elastic.Configurations;
using Nest;
using OrchardCore.Elastic.Search;

namespace OrchardCore.Search.Elastic
{
    /// <summary>
    /// These services are registered on the tenant service collection
    /// </summary>
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;
        private readonly IShellConfiguration _configuration;
        private readonly ILogger<Startup> _logger;

        public Startup(IOptions<AdminOptions> adminOptions,
            IShellConfiguration configuration,
            ILogger<Startup> logger)
        {
            _adminOptions = adminOptions.Value;
            _configuration = configuration;
            _logger = logger;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            var url = _configuration[$"OrchardCore_Elastic:{nameof(ElasticConnectionOptions.Url)}"];
            if (CheckOptions(url, _logger))
            {
                var settings = new ConnectionSettings(new Uri(url));
                var client = new ElasticClient(settings);
                services.AddSingleton<IElasticClient>(client);
            }
            services.AddSingleton<ElasticIndexingState>();
            services.AddSingleton<ElasticIndexSettingsService>();
            services.AddSingleton<ElasticIndexManager>();
            services.AddSingleton<ElasticAnalyzerManager>();
            services.AddScoped<ElasticIndexingService>();
            services.AddScoped<ISearchQueryService, SearchQueryService>();

            services.AddScoped<IContentTypePartDefinitionDisplayDriver, ContentTypePartIndexSettingsDisplayDriver>();
            services.AddScoped<IContentPartFieldDefinitionDisplayDriver, ContentPartFieldIndexSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IPermissionProvider, Permissions>();

            services.Configure<ElasticOptions>(o =>
                o.Analyzers.Add(new ElasticAnalyzer(ElasticSettings.StandardAnalyzer, new StandardAnalyzer())));

            services.AddScoped<IDisplayDriver<ISite>, ElasticSettingsDisplayDriver>();
            services.AddScoped<IDisplayDriver<Query>, ElasticQueryDisplayDriver>();

            services.AddScoped<IContentHandler, ElasticIndexingContentHandler>();
            services.AddElasticQueries();

            // LuceneQuerySource is registered for both the Queries module and local usage
            services.AddScoped<IQuerySource, ElasticQuerySource>();
            services.AddScoped<ElasticQuerySource>();
            services.AddRecipeExecutionStep<ElasticIndexStep>();

            services.AddScoped<IShapeTableProvider, SearchShapesTableProvider>();
            services.AddShapeAttributes<SearchShapes>();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Elastic.Search",
                areaName: "OrchardCore.Search.Elastic",
                pattern: "Search",
                defaults: new { controller = "Search", action = "Search" }
            );

            var adminControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Elastic.Index",
                areaName: "OrchardCore.Search.Elastic",
                pattern: _adminOptions.AdminUrlPrefix + "/elastic/Index",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "Elastic.Delete",
                areaName: "OrchardCore.Search.Elastic",
                pattern: _adminOptions.AdminUrlPrefix + "/Elastic/Delete/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Delete) }
            );

            routes.MapAreaControllerRoute(
                name: "Elastic.Query",
                areaName: "OrchardCore.Search.Elastic",
                pattern: _adminOptions.AdminUrlPrefix + "/Elastic/Query",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Query) }
            );

            routes.MapAreaControllerRoute(
                name: "Elastic.Rebuild",
                areaName: "OrchardCore.Search.Elastic",
                pattern: _adminOptions.AdminUrlPrefix + "/Elastic/Rebuild/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Rebuild) }
            );

            routes.MapAreaControllerRoute(
                name: "Elastic.Reset",
                areaName: "OrchardCore.Search.Elastic",
                pattern: _adminOptions.AdminUrlPrefix + "/Elastic/Reset/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Reset) }
            );
        }

        private static bool CheckOptions(string url, ILogger logger)
        {
            var optionsAreValid = true;

            if (String.IsNullOrWhiteSpace(url))
            {
                logger.LogError("Elastic Search is enabled but not active because the 'Url' is missing or empty in application configuration.");
                optionsAreValid = false;
            }

            return optionsAreValid;
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDeploymentSource, ElasticIndexDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<ElasticIndexDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, ElasticIndexDeploymentStepDriver>();

            services.AddTransient<IDeploymentSource, ElasticSettingsDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<ElasticSettingsDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, ElasticSettingsDeploymentStepDriver>();
        }
    }

    

    [Feature("OrchardCore.Search.Elastic.ContentPicker")]
    public class ElasticContentPickerStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IContentPickerResultProvider, ElasticContentPickerResultProvider>();
            services.AddScoped<IContentPartFieldDefinitionDisplayDriver, ContentPickerFieldElasticEditorSettingsDriver>();
            services.AddShapeAttributes<ElasticContentPickerShapeProvider>();
        }
    }
}
