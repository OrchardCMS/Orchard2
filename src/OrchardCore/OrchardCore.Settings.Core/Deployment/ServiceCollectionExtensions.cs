using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;

namespace OrchardCore.Settings.Deployment
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a <see cref="SiteSettingsPropertyDeploymentSource{TModel}"/> step.
        /// </summary>
        public static void AddSiteSettingsPropertyDeploymentStep<TModel, TLocalizer>(this IServiceCollection services, Func<IStringLocalizer, LocalizedString> title, Func<IStringLocalizer, string> description) where TModel : class, new() where TLocalizer : class
        {
            services.AddTransient<IDeploymentSource, SiteSettingsPropertyDeploymentSource<TModel>>();
            services.AddScoped<IDisplayDriver<DeploymentStep>>(sp =>
            {
                // Do not name this S, it should not be collected up by the po extractor.
                var stringLocalizer = sp.GetService<IStringLocalizer<TLocalizer>>();
                return new SiteSettingsPropertyDeploymentStepDriver<TModel>(title(stringLocalizer), description(stringLocalizer));
            });

            services.AddSingleton<IDeploymentStepFactory>(new SiteSettingsPropertyDeploymentStepFactory<TModel>());
        }
    }
}
