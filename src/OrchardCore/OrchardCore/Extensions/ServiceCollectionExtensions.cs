using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Extensions.Features;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExtensionManagerHost(this IServiceCollection services)
        {
            services.AddSingleton<IExtensionManager, ExtensionManager>();
            {
                services.AddSingleton<ITypeFeatureProvider, TypeFeatureProvider>();
                services.AddSingleton<IFeaturesProvider, FeaturesProvider>();
                services.AddSingleton<IExtensionDependencyStrategy, ExtensionDependencyStrategy>();
                services.AddSingleton<IExtensionPriorityStrategy, ExtensionPriorityStrategy>();
            }

            return services;
        }

        public static IServiceCollection AddExtensionManager(this IServiceCollection services)
        {
            services.TryAddTransient<IFeatureHash, FeatureHash>();

            return services;
        }
    }
}
