using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Localization.Drivers;
using OrchardCore.Localization.Models;
using OrchardCore.Localization.Services;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;

namespace OrchardCore.Localization
{
    /// <summary>
    /// Represents a localization module entry point.
    /// </summary>
    public class Startup : StartupBase
    {
        public override int ConfigureOrder => -100;

        /// <inheritdocs />
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDisplayDriver<ISite>, LocalizationSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<ILocalizationService, LocalizationService>();

            services.AddPortableObjectLocalization(options => options.ResourcesPath = "Localization").
                AddDataAnnotationsPortableObjectLocalization();

            services.Replace(ServiceDescriptor.Singleton<ILocalizationFileLocationProvider, ModularPoFileLocationProvider>());
        }

        /// <inheritdocs />
        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var localizationService = serviceProvider.GetService<ILocalizationService>();

            var defaultCulture = localizationService.GetDefaultCultureAsync().GetAwaiter().GetResult();
            var supportedCultures = localizationService.GetSupportedCulturesAsync().GetAwaiter().GetResult();

            var options = serviceProvider.GetService<IOptions<RequestLocalizationOptions>>().Value;
            options.SetDefaultCulture(defaultCulture);
            options
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures)
                ;

            app.UseRequestLocalization(options);
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class LocalizationDeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDeploymentSource, SiteSettingsPropertyDeploymentSource<LocalizationSettings>>();
            services.AddScoped<IDisplayDriver<DeploymentStep>>(sp =>
            {
                var S = sp.GetService<IStringLocalizer<LocalizationDeploymentStartup>>();
                return new SiteSettingsPropertyDeploymentStepDriver<LocalizationSettings>(S["Culture settings"], S["Exports the culture settings."]);
            });
            services.AddSingleton<IDeploymentStepFactory>(new SiteSettingsPropertyDeploymentStepFactory<LocalizationSettings>());
        }
    }    
}
