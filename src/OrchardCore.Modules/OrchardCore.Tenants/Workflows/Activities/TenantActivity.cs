using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell;
using OrchardCore.Setup.Services;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Activities
{
    public abstract class TenantActivity : Activity
    {
        protected readonly IStringLocalizer S;
     
        protected TenantActivity(
            IShellSettingsManager shellSettingsManager,
            IShellHost shellHost,
            IWorkflowExpressionEvaluator expressionEvaluator,
            IWorkflowScriptEvaluator scriptEvaluator,
            ISetupService setupService,
            IStringLocalizer localizer)
        {
            ShellSettingsManager = shellSettingsManager;
            ShellHost = shellHost;
            ExpressionEvaluator = expressionEvaluator;
            ScriptEvaluator = scriptEvaluator;
            SetupService = setupService;
            S = localizer;
        }

        public WorkflowExpression<string> TenantName
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        protected IShellSettingsManager ShellSettingsManager { get; }
        protected IShellHost ShellHost { get; }
        protected IWorkflowExpressionEvaluator ExpressionEvaluator { get; }
        protected IWorkflowScriptEvaluator ScriptEvaluator { get; }
        protected ISetupService SetupService { get; }

        public override LocalizedString Category => S["Tenant"];

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
