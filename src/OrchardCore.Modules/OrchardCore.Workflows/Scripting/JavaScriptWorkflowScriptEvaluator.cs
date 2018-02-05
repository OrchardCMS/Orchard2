using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.Modules;
using OrchardCore.Scripting;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Evaluators
{
    public class JavaScriptWorkflowScriptEvaluator : IWorkflowScriptEvaluator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IScriptingManager _scriptingManager;
        private readonly IEnumerable<IWorkflowExecutionContextHandler> _workflowContextHandlers;
        private readonly ILogger<JavaScriptWorkflowScriptEvaluator> _logger;

        public JavaScriptWorkflowScriptEvaluator(
            IServiceProvider serviceProvider,
            IScriptingManager scriptingManager,
            IEnumerable<IWorkflowExecutionContextHandler> workflowContextHandlers,
            IStringLocalizer<JavaScriptWorkflowScriptEvaluator> localizer,
            ILogger<JavaScriptWorkflowScriptEvaluator> logger
        )
        {
            _serviceProvider = serviceProvider;
            _scriptingManager = scriptingManager;
            _workflowContextHandlers = workflowContextHandlers;
            _logger = logger;
            T = localizer;
        }

        private IStringLocalizer T { get; }

        public async Task<T> EvaluateAsync<T>(WorkflowExpression<T> expression, WorkflowExecutionContext workflowContext, params IGlobalMethodProvider[] scopedMethodProviders)
        {
            var workflowDefinition = workflowContext.WorkflowDefinitionRecord;
            var directive = $"js:{expression}";
            var expressionContext = new WorkflowExecutionScriptContext(workflowContext);

            await _workflowContextHandlers.InvokeAsync(async x => await x.EvaluatingScriptAsync(expressionContext), _logger);

            var methodProviders = scopedMethodProviders.Concat(expressionContext.ScopedMethodProviders);
            return (T)_scriptingManager.Evaluate(directive, methodProviders);
        }
    }
}
