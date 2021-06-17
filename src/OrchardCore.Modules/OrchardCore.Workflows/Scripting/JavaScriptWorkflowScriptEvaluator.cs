using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.Modules;
using OrchardCore.Scripting;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Evaluators
{
    public class JavaScriptWorkflowScriptEvaluator : IWorkflowScriptEvaluator
    {
        private readonly IScriptingManager _scriptingManager;
        private readonly IEnumerable<IWorkflowExecutionContextHandler> _workflowContextHandlers;
        private readonly ILogger _logger;

        public JavaScriptWorkflowScriptEvaluator(
            IScriptingManager scriptingManager,
            IEnumerable<IWorkflowExecutionContextHandler> workflowContextHandlers,
            ILogger<JavaScriptWorkflowScriptEvaluator> logger
        )
        {
            _scriptingManager = scriptingManager;
            _workflowContextHandlers = workflowContextHandlers;
            _logger = logger;
        }

        public async Task<T> EvaluateAsync<T>(WorkflowExpression<T> expression, WorkflowExecutionContext workflowContext, params IGlobalMethodProvider[] scopedMethodProviders)
        {
            if (String.IsNullOrWhiteSpace(expression.Expression))
            {
                return default;
            }

            var workflowType = workflowContext.WorkflowType;
            var directive = $"js:{expression}";
            var expressionContext = new WorkflowExecutionScriptContext(workflowContext);

            await _workflowContextHandlers.InvokeAsync((h, expressionContext) => h.EvaluatingScriptAsync(expressionContext), expressionContext, _logger);

            var methodProviders = scopedMethodProviders.Concat(expressionContext.ScopedMethodProviders);
            return (T)await _scriptingManager.EvaluateAsync(directive, null, null, methodProviders);
        }
    }
}
