using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace OrchardCore.Scripting
{
    /// <summary>
    /// An implementation of <see cref="IScriptingManager"/> provides services to evaluate
    /// custom scripts.
    /// </summary>
    public interface IScriptingManager
    {
        /// <summary>
        /// Gets the scripting engine with the specified prefix.
        /// </summary>
        /// <param name="prefix">A string representing the engine to return.</param>
        /// <returns>A scripting engine or <code>null</code> if it couldn't be found.</returns>
        IScriptingEngine GetScriptingEngine(string prefix);

        /// <summary>
        /// Executes some prefixed script by looking for a matching scripting engine.
        /// </summary>
        /// <param name="directive">The directive to execute. A directive is made of a </param>
        /// <param name="fileProvider">An optional <see cref="IFileProvider"/> instance.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="scopedMethodProviders">A list of method providers scoped to the script evaluation.</param>
        /// <returns>The result of the script if any.</returns>
        Task<object> EvaluateAsync(string directive, IFileProvider fileProvider, string basePath, IEnumerable<IGlobalMethodProvider> scopedMethodProviders);

        /// <summary>
        /// The list of available method providers for this <see cref="IScriptingManager"/>
        /// instance.
        /// </summary>
        IReadOnlyList<IGlobalMethodProvider> GlobalMethodProviders { get; }
    }
}
