using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OrchardCore.Scripting;

namespace OrchardCore.Recipes
{
    public class SecretsMethodProvider : IGlobalMethodProvider
    {
        private readonly GlobalMethod _secretsGlobalMethod;
        private readonly GlobalMethod _secretsHandlerGlobalMethod;
        private readonly GlobalMethod[] _allMethods;

        public SecretsMethodProvider(JObject secrets)
        {
            _secretsGlobalMethod = new GlobalMethod
            {
                Name = "secrets",
                Method = serviceprovider => (Func<string, object>)(name =>
                {
                    var value = secrets[name]?["Value"]?.Value<string>();

                    return value;
                })
            };

            _secretsHandlerGlobalMethod = new GlobalMethod
            {
                Name = "secretsHandler",
                Method = serviceprovider => (Func<string, object>)(name =>
                {
                    var value = secrets[name]?["Handler"]?.Value<string>();

                    return value;
                })
            };

            _allMethods = new[] { _secretsGlobalMethod, _secretsHandlerGlobalMethod };
        }

        public IScriptingManager ScriptingManager { get; set; }

        public IEnumerable<GlobalMethod> GetMethods()
        {
            return _allMethods;
        }
    }
}
