using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchardCore.Layers.Services;
using OrchardCore.Rules;
using OrchardCore.Rules.Models;
using OrchardCore.Rules.Services;
using OrchardCore.Scripting;
using OrchardCore.Scripting.JavaScript;
using OrchardCore.Tests.Modules.OrchardCore.Rules;

namespace OrchardCore.Benchmark
{
    [MemoryDiagnoser]
    public class RuleBenchmark
    {
        private static readonly IScriptingEngine _engine;
        private static readonly IScriptingScope _scope;
        private static readonly IRuleService _ruleService;
        private static readonly Rule _rule;

        static RuleBenchmark()
        {
            var services = RuleTests.CreateRuleServiceCollection()
                .AddCondition<HomepageCondition, HomepageConditionEvaluator, ConditionFactory<HomepageCondition>>()
                .AddSingleton<IGlobalMethodProvider, DefaultLayersMethodProvider>()
                .AddMemoryCache()
                .AddScripting()
                .AddJavaScriptEngine();

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString("/");
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);

            var serviceProvider = services.BuildServiceProvider();

            var scriptingManager = serviceProvider.GetRequiredService<IScriptingManager>();

            _engine = scriptingManager.GetScriptingEngine("js");
            _scope = _engine.CreateScopeAsync(scriptingManager.GlobalMethodProviders.SelectMany(x => x.GetMethods()), serviceProvider, null, null).GetAwaiter().GetResult();

            _ruleService = serviceProvider.GetRequiredService<IRuleService>();
            _rule = new Rule
            {
                Conditions = new List<Condition>
                {
                    new HomepageCondition
                    {
                        Value = true
                    }
                }
            };
        }

        // Summary 19th May 2021: dotnet run -c Release --filter *RuleBenchmark* --framework netcoreapp3.1 --job short

        //BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.985 (21H1/May2021Update)
        //Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
        //.NET SDK= 6.0.100-preview.3.21202.5
        //  [Host]   : .NET Core 3.1.15 (CoreCLR 4.700.21.21202, CoreFX 4.700.21.21402), X64 RyuJIT
        //  ShortRun : .NET Core 3.1.15 (CoreCLR 4.700.21.21202, CoreFX 4.700.21.21402), X64 RyuJIT

        //Job=ShortRun IterationCount = 3  LaunchCount=1
        //WarmupCount=3

        //|                           Method |     Mean |    Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
        //|--------------------------------- |---------:|---------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
        //| EvaluateIsHomepageWithJavascript | 5.393 us | 3.540 us | 0.1940 us |  1.00 |    0.00 | 0.1373 | 0.0381 |     - |   1,168 B |
        //|       EvaluateIsHomepageWithRule | 1.133 us | 4.381 us | 0.2401 us |  0.21 |    0.04 | 0.0267 | 0.0134 |     - |     224 B |


        // Summary 19th May 2021: dotnet run -c Release --filter *RuleBenchmark* --framework net5.0 --job short

        //BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.985 (21H1/May2021Update)
        //Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
        //.NET SDK= 6.0.100-preview.3.21202.5
        //  [Host]   : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
        //  ShortRun : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

        //Job=ShortRun IterationCount = 3  LaunchCount=1
        //WarmupCount=3

        //|                           Method |       Mean |       Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
        //|--------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
        //| EvaluateIsHomepageWithJavascript | 4,134.1 ns | 14,354.4 ns | 786.81 ns |  1.00 |    0.00 | 0.1221 | 0.0381 |     - |   1,040 B |
        //|       EvaluateIsHomepageWithRule |   662.3 ns |  1,243.9 ns |  68.18 ns |  0.17 |    0.05 | 0.0219 | 0.0086 |     - |     184 B |

        [Benchmark(Baseline = true)]
        public void EvaluateIsHomepageWithJavascript()
        {
            Convert.ToBoolean(_engine.Evaluate(_scope, "isHomepage()"));
        }

        [Benchmark]
        public async Task EvaluateIsHomepageWithRule()
        {
            await _ruleService.EvaluateAsync(_rule);
        }
    }
}
