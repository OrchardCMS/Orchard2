using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.ContentManagement.Cache;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Cache;
using OrchardCore.Environment.Shell.Events;
using YesSql.Indexes;

namespace OrchardCore.ContentManagement
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddContentManagement(this IServiceCollection services)
        {
            services.AddScoped<ICacheContextProvider, ContentDefinitionCacheContextProvider>();
            services.TryAddScoped<IContentDefinitionManager, ContentDefinitionManager>();
            services.TryAddScoped<IContentDefinitionStore, DatabaseContentDefinitionStore>();
            services.TryAddScoped<IContentManager, DefaultContentManager>();
            services.TryAddScoped<IContentManagerSession, DefaultContentManagerSession>();
            services.AddSingleton<IIndexProvider, ContentItemIndexProvider>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentHandler, UpdateContentsHandler>();
            services.AddScoped<IContentHandler, ContentPartHandlerCoordinator>();
            services.AddSingleton<ITypeActivatorFactory<ContentPart>, ContentPartFactory>();
            services.AddSingleton<ITypeActivatorFactory<ContentField>, ContentFieldFactory>();
            services.AddScoped<IShellEventHandler, MigrationsUninstallShellEventHandler>();

            services.AddSingleton<IContentItemIdGenerator, DefaultContentItemIdGenerator>();
            services.AddScoped<IContentHandleManager, ContentHandleManager>();

            services.AddOptions<ContentOptions>();
            services.AddScoped<IContentPartHandlerResolver, ContentPartHandlerResolver>();

            return services;
        }

        public static IServiceCollection AddFileContentDefinitionStore(this IServiceCollection services)
        {
            services.RemoveAll<IContentDefinitionStore>();
            services.AddScoped<IContentDefinitionStore, FileContentDefinitionStore>();
            return services;
        }
    }
}
