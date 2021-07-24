using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Environment.Extensions.Features;

namespace OrchardCore.Environment.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExtensionManagerHost(this IServiceCollection services)
        {
            services.AddSingleton<IExtensionManager, ExtensionManager>();
            services.AddSingleton<ITypeFeatureProvider, TypeFeatureProvider>();
            services.AddSingleton<IFeaturesProvider, FeaturesProvider>();
            services.AddSingleton<IExtensionDependencyStrategy, ExtensionDependencyStrategy>();
            services.AddSingleton<IExtensionPriorityStrategy, ExtensionPriorityStrategy>();

            return services;
        }

        public static IServiceCollection AddExtensionManager(this IServiceCollection services)
        {
            services.TryAddTransient<IFeatureHash, FeatureHash>();

            return services;
        }

        public static IServiceCollection AddExtensionValidation(this IServiceCollection services)
        {
            services.TryAddSingleton<IFeatureValidationService, FeatureValidationService>();

            return services;
        }
    }
}
