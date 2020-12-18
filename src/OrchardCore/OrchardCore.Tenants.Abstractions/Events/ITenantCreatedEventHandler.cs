using System;
using System.Threading.Tasks;
using OrchardCore.Environment.Shell;
using OrchardCore.Tenants.Services;

namespace OrchardCore.Tenants.Events
{
    /// <summary>
    /// Contract that is called when a tenant is created.
    /// </summary>
    public interface ITenantCreatedEventHandler
    {
        Task TenantCreated(TenantContext context);
    }
}
