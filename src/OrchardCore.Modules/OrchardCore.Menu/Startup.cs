using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.Menu.Drivers;
using OrchardCore.Menu.Handlers;
using OrchardCore.Menu.Models;
using OrchardCore.Menu.TagHelpers;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Menu
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IShapeTableProvider, MenuShapes>();
            services.AddScoped<IPermissionProvider, Permissions>();

            // MenuPart
            services.AddScoped<IContentHandler, MenuContentHandler>();
            services.AddScoped<IContentPartDisplayDriver, MenuPartDisplayDriver>();
            services.Configure<ContentPartOptions>(options =>
            {
                options.AddPart<MenuPart>();
                options.AddPart<MenuItemsListPart>();
            });

            // LinkMenuItemPart
            services.AddScoped<IContentPartDisplayDriver, LinkMenuItemPartDisplayDriver>();
            services.Configure<ContentPartOptions>(options =>
            {
                options.AddPart<LinkMenuItemPart>();
            });

            services.AddTagHelpers<MenuTagHelper>();
        }
    }
}
