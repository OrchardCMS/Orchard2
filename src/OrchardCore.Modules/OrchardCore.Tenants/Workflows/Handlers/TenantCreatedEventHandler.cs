using System;
using System.Threading.Tasks;
using OrchardCore.Tenants.Events;
using OrchardCore.Tenants.Services;
using OrchardCore.Tenants.Workflows.Activities;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Handlers
{
    public class TenantCreatedEventHandler: ITenantCreatedEventHandler
    {
        private readonly IWorkflowManager _workflowManager;

        public TenantCreatedEventHandler(IWorkflowManager workflowManager)
        {
            _workflowManager = workflowManager;
        }

        public Task TenantCreated(TenantContext context)
        {
            return _workflowManager.TriggerEventAsync(nameof(TenantCreatedEvent),
                input: new { ShellSettings = context.ShellSettings, EncodedUrl = context.EncodedUrl },
                correlationId: context.ShellSettings.VersionId
            );
        }

    }
}
