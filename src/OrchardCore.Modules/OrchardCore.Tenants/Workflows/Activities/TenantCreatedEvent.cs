using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell;
using OrchardCore.Setup.Services;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Activities
{
    public class TenantCreatedEvent : TenantEvent
    {
        public TenantCreatedEvent(IShellSettingsManager shellSettingsManager, IShellHost shellHost, IWorkflowExpressionEvaluator expressionEvaluator, IWorkflowScriptEvaluator scriptEvaluator, ISetupService setupService, IStringLocalizer<TenantCreatedEvent> localizer)
            : base(shellSettingsManager, shellHost, expressionEvaluator, scriptEvaluator, setupService, localizer)
        {
        }

        public override LocalizedString Category => S["Tenant"];
        public override string Name => nameof(TenantCreatedEvent);
        public override LocalizedString DisplayText => S["Tenant Created Event"];

        public ShellSettings ShellSettings
        {
            get => GetProperty<ShellSettings>();
            set => SetProperty(value);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"]);
        }

        public override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes("Done");
        }

    }
}
