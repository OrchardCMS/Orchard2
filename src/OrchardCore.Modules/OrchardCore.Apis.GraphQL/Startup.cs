using System;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Apis.GraphQL.Controllers;
using OrchardCore.Apis.GraphQL.Services;
using OrchardCore.Apis.GraphQL.ValidationRules;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Apis.GraphQL
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;
        private readonly IHostEnvironment _hostingEnvironment;

        public Startup(IOptions<AdminOptions> adminOptions, IHostEnvironment hostingEnvironment)
        {
            _adminOptions = adminOptions.Value;
            _hostingEnvironment = hostingEnvironment;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>();
            services.AddSingleton<IErrorInfoProvider>(services =>
            {
                var settings = services.GetRequiredService<IOptions<GraphQLSettings>>();
                return new ErrorInfoProvider(new ErrorInfoProviderOptions { ExposeExceptionStackTrace = settings.Value.ExposeExceptions });
            });

            services.AddSingleton<ISchemaFactory, SchemaService>();
            services.AddScoped<IValidationRule, MaxNumberOfResultsValidationRule>();
            services.AddScoped<IValidationRule, RequiresPermissionValidationRule>();

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddTransient<INavigationProvider, AdminMenu>();

            services.AddOptions<GraphQLSettings>().Configure<IShellConfiguration>((c, configuration) =>
            {
                var exposeExceptions = configuration.GetValue(
                    $"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.ExposeExceptions)}",
                    _hostingEnvironment.IsDevelopment());

                var maxNumberOfResultsValidationMode = configuration.GetValue<MaxNumberOfResultsValidationMode?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.MaxNumberOfResultsValidationMode)}")
                                                        ?? MaxNumberOfResultsValidationMode.Default;

                if (maxNumberOfResultsValidationMode == MaxNumberOfResultsValidationMode.Default)
                {
                    maxNumberOfResultsValidationMode = _hostingEnvironment.IsDevelopment() ? MaxNumberOfResultsValidationMode.Enabled : MaxNumberOfResultsValidationMode.Disabled;
                }

                c.BuildUserContext = ctx => new GraphQLUserContext
                {
                    User = ctx.User,
                };
                c.ExposeExceptions = exposeExceptions;
                c.MaxDepth = configuration.GetValue<int?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.MaxDepth)}") ?? 20;
                c.MaxComplexity = configuration.GetValue<int?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.MaxComplexity)}");
                c.FieldImpact = configuration.GetValue<double?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.FieldImpact)}");
                c.MaxNumberOfResults = configuration.GetValue<int?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.MaxNumberOfResults)}") ?? 1000;
                c.MaxNumberOfResultsValidationMode = maxNumberOfResultsValidationMode;
                c.DefaultNumberOfResults = configuration.GetValue<int?>($"OrchardCore_Apis_GraphQL:{nameof(GraphQLSettings.DefaultNumberOfResults)}") ?? 100;
            });
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "GraphQL",
                areaName: "OrchardCore.Apis.GraphQL",
                pattern: _adminOptions.AdminUrlPrefix + "/GraphQL",
                defaults: new { controller = typeof(AdminController).ControllerName(), action = nameof(AdminController.Index) }
            );

            app.UseMiddleware<GraphQLMiddleware>(serviceProvider.GetService<IOptions<GraphQLSettings>>().Value);
        }
    }
}
