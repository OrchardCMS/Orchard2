using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentLocalization.Drivers;
using OrchardCore.ContentLocalization.Handlers;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentLocalization.Records;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data;
using OrchardCore.Data.Migration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddContentLocalization(this IServiceCollection services)
        {
            services.AddContentPart<LocalizationPart>()
                .UseDisplayDriver<LocalizationPartDisplayDriver>()
                .AddHandler<LocalizationPartHandler>();

            services.TryAddScoped<IContentLocalizationManager, DefaultContentLocalizationManager>();

            services.AddScoped<LocalizedContentItemIndexProvider>();
            services.AddScoped<IScopedIndexProvider>(sp => sp.GetRequiredService<LocalizedContentItemIndexProvider>());
            services.AddScoped<IContentHandler>(sp => sp.GetRequiredService<LocalizedContentItemIndexProvider>());

            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentLocalizationHandler, ContentLocalizationPartHandlerCoordinator>();

            return services;
        }
    }
}
