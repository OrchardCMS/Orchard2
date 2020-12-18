using OrchardCore.Tenants.Workflows.Activities;
using OrchardCore.Workflows.ViewModels;

namespace OrchardCore.Tenants.Workflows.ViewModels
{
    public class TenantEventViewModel<T> : ActivityViewModel<T> where T : TenantEvent
    {
        public TenantEventViewModel()
        {
        }

        public TenantEventViewModel(T activity)
        {
            Activity = activity;
        }
    }
}