using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace OrchardCore.Scripting.Files
{
    /// <summary>
    /// Provides
    /// </summary>
    public class FilesScriptEngine : IScriptingEngine
    {
        public string Prefix => "file";

        public Task<IScriptingScope> CreateScopeAsync(IEnumerable<GlobalMethod> methods, IServiceProvider serviceProvider, IFileProvider fileProvider, string basePath)
        {
            return Task.FromResult<IScriptingScope>(new FilesScriptScope(fileProvider, basePath));
        }

        public object Evaluate(IScriptingScope scope, string script)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (!(scope is FilesScriptScope fileScope))
            {
                throw new ArgumentException($"Expected a scope of type {nameof(FilesScriptScope)}", nameof(scope));
            }

            if (script.StartsWith("text('", StringComparison.Ordinal) && script.EndsWith("')", StringComparison.Ordinal))
            {
                var filePath = script.Substring(6, script.Length - 8);
                var fileInfo = fileScope.FileProvider.GetRelativeFileInfo(fileScope.BasePath, filePath);
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(filePath);
                }

                using (var fileStream = fileInfo.CreateReadStream())
                {
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            else if (script.StartsWith("base64('", StringComparison.Ordinal) && script.EndsWith("')", StringComparison.Ordinal))
            {
                var filePath = script.Substring(8, script.Length - 10);
                var fileInfo = fileScope.FileProvider.GetRelativeFileInfo(fileScope.BasePath, filePath);
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(filePath);
                }

                using (var fileStream = fileInfo.CreateReadStream())
                {
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Unknown command '{script}'");
            }
        }
    }
}
