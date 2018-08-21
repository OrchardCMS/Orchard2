using System;
using System.Linq;
using OrchardCore.Workflows.Models;
using YesSql.Indexes;

namespace OrchardCore.Workflows.Indexes
{
    public class WorkflowIndex : MapIndex
    {
        public string WorkflowTypeId { get; set; }
        public string WorkflowId { get; set; }
        public string WorkflowCorrelationId { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public class WorkflowBlockingActivitiesIndex : WorkflowIndex
    {
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public bool ActivityIsStart { get; set; }
    }

    public class WorkflowIndexProvider : IndexProvider<Workflow>
    {
        public override void Describe(DescribeContext<Workflow> context)
        {
            context.For<WorkflowIndex>()
                .Map(workflow =>
                    new WorkflowIndex
                    {
                        WorkflowTypeId = workflow.WorkflowTypeId,
                        WorkflowId = workflow.WorkflowId,
                        CreatedUtc = workflow.CreatedUtc
                    }
                );

            context.For<WorkflowBlockingActivitiesIndex>()
                .Map(workflow =>
                    workflow.BlockingActivities.Select(x =>
                    new WorkflowBlockingActivitiesIndex
                    {
                        ActivityId = x.ActivityId,
                        ActivityName = x.Name,
                        ActivityIsStart = x.IsStart,
                        WorkflowTypeId = workflow.WorkflowTypeId,
                        WorkflowId = workflow.WorkflowId,
                        WorkflowCorrelationId = workflow.CorrelationId ?? ""
                    })
                );
        }
    }
}
