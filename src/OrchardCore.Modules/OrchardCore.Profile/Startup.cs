using System;
using Microsoft.AspNetCore.Builder;
using OrchardCore.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Security.Permissions;
using OrchardCore.Profile.Service;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement;
using OrchardCore.Profile.Navigation;
using OrchardCore.DisplayManagement.Descriptors;
using Microsoft.AspNetCore.Mvc;

namespace OrchardCore.Profile
{
    public class Startup : StartupBase
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddNavigation();

            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(ProfileMenuFilter));
            });

            services.AddScoped<IShapeTableProvider, ProfileNavigationShapes>();

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddSingleton<IProfileService, ProfileService>();
            services.AddScoped<IDisplayManager<IProfile>, DisplayManager<IProfile>>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "EndOrchardCore.ProfileWithGroupId",
                areaName: "OrchardCore.Profile",
                pattern: "Profile/{groupId}",
                defaults: new { controller = "Profile", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "EndOrchardCore.Profile",
                areaName: "OrchardCore.Profile",
                pattern: "Profile",
                defaults: new { controller = "Profile", action = "Index" }
            );
        }
    }
}
