using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Navigation;
using OrchardCore.Google.Authentication.Configuration;
using OrchardCore.Google.Authentication.Drivers;
using OrchardCore.Google.Authentication.Services;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Google.Analytics;
using OrchardCore.Google.Analytics.Drivers;

namespace OrchardCore.Google
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, Permissions>();
        }
    }

    [Feature(GoogleConstants.Features.GoogleAuthentication)]
    public class GoogleAuthenticationStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<GoogleAuthenticationService, GoogleAuthenticationService>();
            services.AddScoped<IDisplayDriver<ISite>, GoogleAuthenticationSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenuGoogleAuthentication>();
            // Register the options initializers required by the Google Handler.
            services.TryAddEnumerable(new[]
            {
                // Orchard-specific initializers:
                ServiceDescriptor.Transient<IConfigureOptions<AuthenticationOptions>, GoogleOptionsConfiguration>(),
                ServiceDescriptor.Transient<IConfigureOptions<GoogleOptions>, GoogleOptionsConfiguration>(),
                // Built-in initializers:
                ServiceDescriptor.Transient<IPostConfigureOptions<GoogleOptions>, OAuthPostConfigureOptions<GoogleOptions,GoogleHandler>>()
            });
        }
    }

    [Feature(GoogleConstants.Features.GoogleAnalytics)]
    public class GoogleAnalyticsStartup : StartupBase
    {

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDisplayDriver<ISite>, GoogleAnalyticsSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenuGoogleAnalytics>();
            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(GoogleAnalyticsFilter));
            });
        }

    }

}
