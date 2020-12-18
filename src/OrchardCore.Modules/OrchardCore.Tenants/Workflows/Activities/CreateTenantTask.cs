using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;
using OrchardCore.Setup.Services;
using OrchardCore.Tenants.Events;
using OrchardCore.Tenants.Services;
using OrchardCore.Tenants.Utilities;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Tenants.Workflows.Activities
{
    public class CreateTenantTask : TenantTask
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public CreateTenantTask(
            IShellSettingsManager shellSettingsManager,
            IShellHost shellHost,
            IWorkflowExpressionEvaluator expressionEvaluator,
            IWorkflowScriptEvaluator scriptEvaluator,
            IServiceProvider serviceProvider,
            ISetupService setupService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateTenantTask> logger,
            IStringLocalizer<CreateTenantTask> localizer
        ) : base(shellSettingsManager, shellHost, expressionEvaluator, scriptEvaluator, setupService, localizer)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override string Name => nameof(CreateTenantTask);

        public override LocalizedString Category => S["Tenant"];

        public override LocalizedString DisplayText => S["Create Tenant Task"];

        public string ContentType
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public WorkflowExpression<string> Description
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> RequestUrlPrefix
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> RequestUrlHost
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> DatabaseProvider
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> ConnectionString
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> TablePrefix
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> RecipeName
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"], S["Failed"]);
        }

        public async override Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            if (ShellScope.Context.Settings.Name != ShellHelper.DefaultShellName)
            {
                return Outcomes("Failed");
            }

            var tenantName = (await ExpressionEvaluator.EvaluateAsync(TenantName, workflowContext, null))?.Trim();

            if (string.IsNullOrEmpty(tenantName))
            {
                return Outcomes("Failed");
            }

            if (ShellHost.TryGetSettings(tenantName, out var shellSettings))
            {
                return Outcomes("Failed");
            }

            var requestUrlPrefix = (await ExpressionEvaluator.EvaluateAsync(RequestUrlPrefix, workflowContext, null))?.Trim();
            var requestUrlHost = (await ExpressionEvaluator.EvaluateAsync(RequestUrlHost, workflowContext, null))?.Trim();
            var databaseProvider = (await ExpressionEvaluator.EvaluateAsync(DatabaseProvider, workflowContext, null))?.Trim();
            var connectionString = (await ExpressionEvaluator.EvaluateAsync(ConnectionString, workflowContext, null))?.Trim();
            var tablePrefix = (await ExpressionEvaluator.EvaluateAsync(TablePrefix, workflowContext, null))?.Trim();
            var recipeName = (await ExpressionEvaluator.EvaluateAsync(RecipeName, workflowContext, null))?.Trim();

            // Creates a default shell settings based on the configuration.
            shellSettings = ShellSettingsManager.CreateDefaultSettings();

            shellSettings.Name = tenantName;

            if (!string.IsNullOrEmpty(requestUrlHost))
            {
                shellSettings.RequestUrlHost = requestUrlHost;
            }

            if (!string.IsNullOrEmpty(requestUrlPrefix))
            {
                shellSettings.RequestUrlPrefix = requestUrlPrefix;
            }

            shellSettings.State = TenantState.Uninitialized;

            if (!string.IsNullOrEmpty(connectionString))
            {
                shellSettings["ConnectionString"] = connectionString;
            }

            if (!string.IsNullOrEmpty(tablePrefix))
            {
                shellSettings["TablePrefix"] = tablePrefix;
            }

            if (!string.IsNullOrEmpty(databaseProvider))
            {
                shellSettings["DatabaseProvider"] = databaseProvider;
            }

            if (!string.IsNullOrEmpty(recipeName))
            {
                shellSettings["RecipeName"] = recipeName;
            }

            shellSettings["Secret"] = Guid.NewGuid().ToString();

            await ShellHost.UpdateShellSettingsAsync(shellSettings);

            // Invoke modules to react to the tenant created event
            var token = SetupService.CreateSetupToken(shellSettings);
            var encodedUrl = TenantHelperExtensions.GetEncodedUrl(shellSettings, _httpContextAccessor.HttpContext.Request, token);
            var context = new TenantContext
            {
                ShellSettings = shellSettings,
                EncodedUrl = encodedUrl
            };
            var tenantCreatedEventHandlers = _serviceProvider.GetRequiredService<IEnumerable<ITenantCreatedEventHandler>>();
            var logger = _serviceProvider.GetRequiredService<ILogger<CreateTenantTask>>();
            if (tenantCreatedEventHandlers.Any())
            {
                await tenantCreatedEventHandlers.InvokeAsync((handler, conext) => handler.TenantCreated(context), context, logger);
            }
            workflowContext.LastResult = shellSettings;
            workflowContext.CorrelationId = shellSettings.Name;

            return Outcomes("Done");
        }
    }
}
