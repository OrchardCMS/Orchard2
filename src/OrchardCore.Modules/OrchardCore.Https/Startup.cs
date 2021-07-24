using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Https.Drivers;
using OrchardCore.Https.Services;
using OrchardCore.Https.Settings;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;

namespace OrchardCore.Https
{
    public class Startup : StartupBase
    {
        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetRequiredService<IHttpsService>();
            var settings = service.GetSettingsAsync().GetAwaiter().GetResult();
            if (settings.RequireHttps)
            {
                app.UseHttpsRedirection();
            }

            if (settings.EnableStrictTransportSecurity)
            {
                app.UseHsts();
            }
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IDisplayDriver<ISite>, HttpsSettingsDisplayDriver>();
            services.AddSingleton<IHttpsService, HttpsService>();

            services.AddScoped<IPermissionProvider, Permissions>();

            services.AddOptions<HttpsRedirectionOptions>()
                .Configure<IHttpsService>((options, service) =>
                {
                    var settings = service.GetSettingsAsync().GetAwaiter().GetResult();
                    if (settings.RequireHttpsPermanent)
                    {
                        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    }

                    if (settings.SslPort != null)
                    {
                        options.HttpsPort = settings.SslPort;
                    }
                });

            services.AddHsts(options =>
            {
                options.Preload = false;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<HttpsSettings, DeploymentStartup>(S => S["Https settings"], S => S["Exports the Https settings."]);
        }
    }
}
