using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell;
using OrchardCore.Setup.Services;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Activities
{
    public abstract class TenantTask : TenantActivity, ITask
    {
        protected TenantTask(IShellSettingsManager shellSettingsManager, IShellHost shellHost, IWorkflowExpressionEvaluator expressionEvaluator, IWorkflowScriptEvaluator scriptEvaluator,
            ISetupService setupService, IStringLocalizer localizer)
            : base(shellSettingsManager, shellHost, expressionEvaluator, scriptEvaluator, setupService, localizer)
        {
        }
    }
}
