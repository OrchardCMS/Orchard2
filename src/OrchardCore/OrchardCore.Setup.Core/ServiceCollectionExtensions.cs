using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Entities;
using OrchardCore.Setup.Core;
using OrchardCore.Setup.Services;

namespace OrchardCore.Setup
{
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds tenant level services.
        /// </summary>
        public static IServiceCollection AddSetup(this IServiceCollection services)
        {
            services.TryAddScoped<IUrlService, UrlService>();
            services.TryAddScoped<ISetupService, SetupService>();

            services.AddIdGeneration();
            services.TryAddSingleton<ISetupUserIdGenerator, SetupUserIdGenerator>();

            return services;
        }
    }
}
