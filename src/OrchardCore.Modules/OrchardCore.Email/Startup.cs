using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Email.Drivers;
using OrchardCore.Email.Models;
using OrchardCore.Email.Services;
using OrchardCore.Environment.Navigation;
using OrchardCore.Modules;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;

namespace OrchardCore.Email
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IDisplayDriver<ISite>, SmtpSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddScoped<IConfigureOptions<SmtpSettings>, SmtpSettingsConfiguration>();
            services.AddScoped<ISmtpService, SmtpService>();
        }
    }
}
