using OrchardCore.Workflows.Activities;
using OrchardCore.Secrets;

namespace OrchardCore.Workflows.Http.Models
{
    public class HttpRequestEventSecret : Secret
    {
        public string WorkflowTypeId { get; set; }
        public string ActivityId { get; set; }
        public int TokenLifeSpan { get; set; }
    }
}
