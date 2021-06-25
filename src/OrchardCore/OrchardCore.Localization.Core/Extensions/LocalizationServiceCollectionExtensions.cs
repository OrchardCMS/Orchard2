using System;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using OrchardCore.Localization;
using OrchardCore.Localization.Data;
using OrchardCore.Localization.PortableObject;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class LocalizationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the services to enable localization using Portable Object files.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static IServiceCollection AddPortableObjectLocalization(this IServiceCollection services)
        {
            return AddPortableObjectLocalization(services, null);
        }

        /// <summary>
        /// Registers the services to enable localization using Portable Object files.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="setupAction">An action to configure the Microsoft.Extensions.Localization.LocalizationOptions.</param>
        public static IServiceCollection AddPortableObjectLocalization(this IServiceCollection services, Action<LocalizationOptions> setupAction)
        {
            services.AddSingleton<IPluralRuleProvider, DefaultPluralRuleProvider>();
            services.AddSingleton<ITranslationProvider, PoFilesTranslationsProvider>();
            services.AddSingleton<ILocalizationFileLocationProvider, ContentRootPoFileLocationProvider>();
            services.AddSingleton<ILocalizationManager, LocalizationManager>();
            services.AddSingleton<IStringLocalizerFactory, PortableObjectStringLocalizerFactory>();
            services.AddSingleton<IHtmlLocalizerFactory, PortableObjectHtmlLocalizerFactory>();
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }

        /// <summary>
        /// Registers the services to enable localization using data storage.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static IServiceCollection AddDataLocalization(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IPluralRuleProvider, DefaultPluralRuleProvider>();
            services.AddSingleton<ITranslationProvider, DataTranslationsProvider>();
            services.AddSingleton<ILocalizationManager, LocalizationManager>();
            services.AddSingleton<IDataLocalizerFactory, DataLocalizerFactory>();
            services.AddTransient(sp => {
                var dataLocalizerFactory = sp.GetService<IDataLocalizerFactory>();

                return dataLocalizerFactory.Create();
            });

            return services;
        }
    }
}
