using OrchardCore.DisplayManagement.Views;
using OrchardCore.Tenants.Services;
using OrchardCore.Tenants.Workflows.Activities;
using OrchardCore.Tenants.Workflows.ViewModels;
using OrchardCore.Workflows.Display;

namespace OrchardCore.Tenants.Workflows.Drivers
{
    public class TenantCreatedEventDisplayDriver : ActivityDisplayDriver<TenantCreatedEvent, TenantCreatedEventViewModel>
    {

        protected override void EditActivity(TenantCreatedEvent source, TenantCreatedEventViewModel target)
        {
        }

        public override IDisplayResult Display(TenantCreatedEvent activity)
        {
            return Combine(
                Shape("TenantCreatedEvent_Fields_Thumbnail", new TenantCreatedEventViewModel(activity)).Location("Thumbnail", "Content"),
                Factory("TenantCreatedEvent_Fields_Design", ctx =>
                {
                    var shape = new TenantCreatedEventViewModel();
                    shape.Activity = activity;

                    return shape;
                }).Location("Design", "Content")
            );
        }
    }
}
