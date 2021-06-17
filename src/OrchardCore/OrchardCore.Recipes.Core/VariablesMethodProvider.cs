using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Scripting;

namespace OrchardCore.Recipes
{
    public class VariablesMethodProvider : IGlobalMethodProvider
    {
        private readonly GlobalMethod _globalMethod;

        public VariablesMethodProvider(JObject variables, List<IGlobalMethodProvider> scopedMethodProviders)
        {
            _globalMethod = new GlobalMethod
            {
                Name = "variables",
                Method = serviceprovider => (Func<string, object>)(name =>
                {
                    var value = variables[name].Value<string>();

                    // Replace variable value while the result returns another script
                    while (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        value = value.Trim('[', ']');
                        value = (ScriptingManager.EvaluateAsync(value, null, null, scopedMethodProviders).GetAwaiter().GetResult() ?? "").ToString();
                        variables[name] = new JValue(value);
                    }

                    return value;
                })
            };
        }

        public IScriptingManager ScriptingManager => ShellScope.Services.GetRequiredService<IScriptingManager>();

        public IEnumerable<GlobalMethod> GetMethods()
        {
            yield return _globalMethod;
        }
    }
}
