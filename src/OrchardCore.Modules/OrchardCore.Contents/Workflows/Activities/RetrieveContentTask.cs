using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Contents.Workflows.Activities
{
    public class RetrieveContentTask : ContentTask
    {
        public RetrieveContentTask(IContentManager contentManager, IWorkflowScriptEvaluator scriptEvaluator, IStringLocalizer<RetrieveContentTask> localizer) : base(contentManager, scriptEvaluator, localizer)
        {
        }

        public override string Name => nameof(RetrieveContentTask);

        public override LocalizedString DisplayText => S["Retrieve Content Task"];

        public override LocalizedString Category => S["Content"];

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Retrieved"]);
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var contentItemId = await GetContentItemIdAsync(workflowContext);

            // Clean up return string if passed expression was malformed due to trying to add a string: variable + ""
            contentItemId = contentItemId
                .Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\\", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Trim();

            var contentItem = await ContentManager.GetAsync(contentItemId, VersionOptions.Latest);
            if (contentItem == null)
                return Outcomes("Not Found");

            workflowContext.LastResult = contentItem;

            return Outcomes("Retrieved");
        }
    }
}
