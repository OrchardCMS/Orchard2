using OrchardCore.BackgroundTasks;
using OrchardCore.Data;
using OrchardCore.DeferredTasks;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Liquid;
using OrchardCore.Environment.Cache;
using OrchardCore.Environment.Commands;
using OrchardCore.Environment.Shell.Data;
using OrchardCore.Modules;
using OrchardCore.Mvc;
using OrchardCore.ResourceManagement;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddOrchardCms(this IServiceCollection services)
        {
            return services

                .AddOrchardCore(builder => builder
                    .AddCommands()

                    .AddMvc()
                    .AddSecurity()
                    .WithDefaultFeatures("OrchardCore.Setup")

                    .AddDataAccess()
                    .AddDataStorage()
                    .AddBackgroundTasks()
                    .AddDeferredTasks()

                    .AddTheming()
                    .AddLiquidViews()
                    .AddResourceManagement()
                    .AddCaching());
        }
    }
}
