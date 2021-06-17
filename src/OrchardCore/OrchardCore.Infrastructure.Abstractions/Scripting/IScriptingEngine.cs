using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace OrchardCore.Scripting
{
    public interface IScriptingEngine
    {
        string Prefix { get; }
        object Evaluate(IScriptingScope scope, string script);
        Task<IScriptingScope> CreateScopeAsync(IEnumerable<GlobalMethod> methods, IServiceProvider serviceProvider, IFileProvider fileProvider, string basePath);
    }
}
