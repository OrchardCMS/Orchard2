using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Tenants.Events;
using OrchardCore.Tenants.Workflows.Activities;
using OrchardCore.Tenants.Workflows.Drivers;
using OrchardCore.Tenants.Workflows.Handlers;
using OrchardCore.Workflows.Helpers;

namespace OrchardCore.Tenants.Workflows
{
    [RequireFeatures("OrchardCore.Workflows")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddActivity<DisableTenantTask, DisableTenantTaskDisplayDriver>();
            services.AddActivity<EnableTenantTask, DisableTenantTaskDisplayDriver>();
            services.AddActivity<CreateTenantTask, DisableTenantTaskDisplayDriver>();
            services.AddActivity<SetupTenantTask, DisableTenantTaskDisplayDriver>();
            services.AddActivity<TenantCreatedEvent, TenantCreatedEventDisplayDriver>();
            services.AddScoped<ITenantCreatedEventHandler, TenantCreatedEventHandler>();
        }
    }
}
