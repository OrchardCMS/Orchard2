using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin.Controllers;
using OrchardCore.Admin.Drivers;
using OrchardCore.Admin.Models;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Mvc.Routing;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;

namespace OrchardCore.Admin
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;
        private readonly IShellConfiguration _configuration;

        public Startup(IOptions<AdminOptions> adminOptions, IShellConfiguration configuration)
        {
            _adminOptions = adminOptions.Value;
            _configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddNavigation();

            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(AdminFilter));
                options.Filters.Add(typeof(AdminMenuFilter));

                // Ordered to be called before any global filter.
                options.Filters.Add(typeof(AdminZoneFilter), -1000);
            });

            services.AddTransient<IAreaControllerRouteMapper, AdminAreaControllerRouteMapper>();
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IThemeSelector, AdminThemeSelector>();
            services.AddScoped<IAdminThemeService, AdminThemeService>();
            services.AddScoped<IDisplayDriver<ISite>, AdminSiteSettingsDisplayDriver>();
            services.AddScoped<IPermissionProvider, PermissionsAdminSettings>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddSingleton<IPageRouteModelProvider, AdminPageRouteModelProvider>();

            services.Configure<AdminOptions>(_configuration.GetSection("OrchardCore_Admin"));
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Admin",
                areaName: "OrchardCore.Admin",
                pattern: _adminOptions.AdminUrlPrefix,
                defaults: new { controller = typeof(AdminController).ControllerName(), action = nameof(AdminController.Index) }
            );
        }
    }

    public class AdminPagesStartup : StartupBase
    {
        public override int Order => 1000;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RazorPagesOptions>((options) =>
            {
                var adminOptions = ShellScope.Services.GetRequiredService<IOptions<AdminOptions>>().Value;
                options.Conventions.Add(new AdminPageRouteModelConvention(adminOptions.AdminUrlPrefix));
            });
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<AdminSettings, DeploymentStartup>(S => S["Admin settings"], S => S["Exports the admin settings."]);
        }
    }
}
