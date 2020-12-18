using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Setup.Services;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Activities
{
    public class DisableTenantTask : TenantTask
    {
        public DisableTenantTask(
            IShellSettingsManager shellSettingsManager,
            IShellHost shellHost,
            IWorkflowExpressionEvaluator expressionEvaluator,
            IWorkflowScriptEvaluator scriptEvaluator,
            ISetupService setupService,
            IStringLocalizer<DisableTenantTask> localizer
        ): base(shellSettingsManager, shellHost, expressionEvaluator, scriptEvaluator, setupService, localizer)
        {
        }
        public override string Name => nameof(DisableTenantTask);

        public override LocalizedString Category => S["Tenant"];

        public override LocalizedString DisplayText => S["Disable Tenant Task"];

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Disabled"], S["Failed"]);
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            if (ShellScope.Context.Settings.Name != ShellHelper.DefaultShellName)
            {
                return Outcomes("Failed");
            }

            var tenantName = (await ExpressionEvaluator.EvaluateAsync(TenantName, workflowContext, null))?.Trim();

            if (tenantName == ShellHelper.DefaultShellName)
            {
                return Outcomes("Failed");
            }

            if (!ShellHost.TryGetSettings(tenantName?.Trim(), out var shellSettings))
            {
                return Outcomes("Failed");
            }

            if (shellSettings.State != TenantState.Running)
            {
                return Outcomes("Failed");
            }

            shellSettings.State = TenantState.Disabled;
            await ShellHost.UpdateShellSettingsAsync(shellSettings);

            return Outcomes("Disabled");
        }
    }
}
