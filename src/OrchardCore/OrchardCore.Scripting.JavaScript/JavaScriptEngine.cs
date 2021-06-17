using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Esprima;
using Jint;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Modules;
using TimeZoneConverter;

namespace OrchardCore.Scripting.JavaScript
{
    public class JavaScriptEngine : IScriptingEngine
    {
        private readonly IMemoryCache _memoryCache;
        public JavaScriptEngine(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public string Prefix => "js";

        public async Task<IScriptingScope> CreateScopeAsync(IEnumerable<GlobalMethod> methods, IServiceProvider serviceProvider, IFileProvider fileProvider, string basePath)
        {
            var localClock = serviceProvider.GetService<ILocalClock>();

            var localTimeZone = await localClock.GetLocalTimeZoneAsync();

            Engine engine;

            if (TZConvert.TryGetTimeZoneInfo(localTimeZone.TimeZoneId, out var timeZoneInfo))
            {
               engine = new Engine(cfg => cfg.LocalTimeZone(timeZoneInfo));
            }
            else
            {
                engine = new Engine();
            }

            foreach (var method in methods)
            {
                engine.SetValue(method.Name, method.Method(serviceProvider));
            }

            return new JavaScriptScope(engine, serviceProvider);
        }

        public object Evaluate(IScriptingScope scope, string script)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var jsScope = scope as JavaScriptScope;

            if (jsScope == null)
            {
                throw new ArgumentException($"Expected a scope of type {nameof(JavaScriptScope)}", nameof(scope));
            }

            var parsedAst = _memoryCache.GetOrCreate(script, entry =>
            {
                var parser = new JavaScriptParser(script);
                return parser.ParseScript();
            });

            var result = jsScope.Engine.Evaluate(parsedAst)?.ToObject();

            return result;
        }
    }

    public class MethodProxy
    {
        public IList<object> Arguments { get; set; }
        public Func<IServiceProvider, IList<object>, object> Callback { get; set; }
        public object Invoke()
        {
            return Callback(null, Arguments);
        }
    }
}
