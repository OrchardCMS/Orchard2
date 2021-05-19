using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell.Events;
using OrchardCore.Media.Core.Events;

namespace OrchardCore.Media.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediaEventHandlers(this IServiceCollection services)
        {
            services.AddScoped<IShellEventHandler, DeleteMediaShellEventHandler>();

            return services;
        }
    }
}
