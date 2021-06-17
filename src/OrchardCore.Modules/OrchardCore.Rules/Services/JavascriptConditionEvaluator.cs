using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Rules.Models;
using OrchardCore.Scripting;

namespace OrchardCore.Rules.Services
{
    public class JavascriptConditionEvaluator : ConditionEvaluator<JavascriptCondition>
    {
        private readonly IScriptingManager _scriptingManager;
        private readonly IServiceProvider _serviceProvider;

        // The scope is built lazily once per request.
        private IScriptingScope _scope;
        private IScriptingEngine _engine;

        public JavascriptConditionEvaluator(IScriptingManager scriptingManager, IServiceProvider serviceProvider)
        {
            _scriptingManager = scriptingManager;
            _serviceProvider = serviceProvider;
        }

        public override async ValueTask<bool> EvaluateAsync(JavascriptCondition condition)
        {
            _engine ??= _scriptingManager.GetScriptingEngine("js");
            _scope ??= await _engine.CreateScopeAsync(_scriptingManager.GlobalMethodProviders.SelectMany(x => x.GetMethods()), _serviceProvider, null, null);

            return Convert.ToBoolean(_engine.Evaluate(_scope, condition.Script));
        }
    }
}
