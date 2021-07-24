using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ReCaptcha.Core;
using OrchardCore.ReCaptcha.Drivers;
using OrchardCore.ReCaptcha.Users.Handlers;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;
using OrchardCore.Users.Events;

namespace OrchardCore.ReCaptcha
{
    [Feature("OrchardCore.ReCaptcha")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddReCaptcha();

            services.AddScoped<IDisplayDriver<ISite>, ReCaptchaSettingsDisplayDriver>();
            services.AddScoped<INavigationProvider, AdminMenu>();
        }
    }

    [Feature("OrchardCore.ReCaptcha")]
    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<ReCaptchaSettings, DeploymentStartup>(S => S["ReCaptcha settings"], S => S["Exports the ReCaptcha settings."]);
        }
    }

    [Feature("OrchardCore.ReCaptcha.Users")]
    public class StartupUsers : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IRegistrationFormEvents, RegistrationFormEventHandler>();
            services.AddScoped<ILoginFormEvent, LoginFormEventEventHandler>();
            services.AddScoped<IPasswordRecoveryFormEvents, PasswordRecoveryFormEventEventHandler>();
            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(ReCaptchaLoginFilter));
            });
        }
    }
}
