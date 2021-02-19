using OrchardCore.Workflows.Activities;

namespace OrchardCore.Workflows.ViewModels
{
    public class ActivityEditViewModel
    {
        public dynamic ActivityEditor { get; set; }
        public IActivity Activity { get; set; }
        public string ActivityId { get; set; }
        public int WorkflowTypeId { get; set; }
        public string WorkflowTypeUniqueId { get; set; }
        public string ReturnUrl { get; set; }
    }
}
