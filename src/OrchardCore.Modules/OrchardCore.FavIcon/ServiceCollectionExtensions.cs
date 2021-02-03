using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.FavIcon.Configuration;
using OrchardCore.FavIcon.Drivers;
using OrchardCore.FavIcon.TagHelpers;
using OrchardCore.Modules;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;
using OrchardCore.Navigation;
using OrchardCore.FavIcon.Liquid;
using Fluid;
using OrchardCore.DisplayManagement.Liquid;

namespace OrchardCore.FavIcon
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFavIcon(this IServiceCollection services, Action<FavIconSettings> configure = null)
        {
            services.AddTransient<IConfigureOptions<FavIconSettings>, FavIconSettingsConfiguration>();
            services.AddTagHelpers<FavIconTagHelper>();

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }

    [Feature("OrchardCore.FavIcon")]
    public class Startup : StartupBase
    {
        static Startup() => BaseFluidTemplate<LiquidViewTemplate>.Factory.RegisterTag<FavIconTag>("favicon");

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddFavIcon();

            services.AddScoped<IDisplayDriver<ISite>, FavIconSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            
        }
    }

    [Feature("OrchardCore.FavIcon")]
    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDeploymentSource, SiteSettingsPropertyDeploymentSource<FavIconSettings>>();
            services.AddScoped<IDisplayDriver<DeploymentStep>>(sp =>
            {
                var S = sp.GetService<IStringLocalizer<DeploymentStartup>>();
                return new SiteSettingsPropertyDeploymentStepDriver<FavIconSettings>(S["FavIcon settings"], S["Exports the FavIcon settings."]);
            });
            services.AddSingleton<IDeploymentStepFactory>(new SiteSettingsPropertyDeploymentStepFactory<FavIconSettings>());
        }
    }
}
