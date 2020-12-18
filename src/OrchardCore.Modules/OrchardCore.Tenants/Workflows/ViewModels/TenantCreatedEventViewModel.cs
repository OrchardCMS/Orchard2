using OrchardCore.Tenants.Workflows.Activities;

namespace OrchardCore.Tenants.Workflows.ViewModels
{
    public class TenantCreatedEventViewModel : TenantEventViewModel<TenantCreatedEvent>
    {
        public TenantCreatedEventViewModel()
        {
        }

        public TenantCreatedEventViewModel(TenantCreatedEvent activity)
        {
            Activity = activity;
        }
    }
}