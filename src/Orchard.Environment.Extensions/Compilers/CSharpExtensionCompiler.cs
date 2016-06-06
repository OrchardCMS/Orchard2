using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Files;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;

namespace Orchard.Environment.Extensions.Compilers
{
    public class CSharpExtensionCompiler
    {
        private static readonly ConcurrentDictionary<string, bool> _compiledLibraries = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Lazy<Assembly> _entryAssembly = new Lazy<Assembly>(Assembly.GetEntryAssembly);

        public CSharpExtensionCompiler ()
        {
            Diagnostics = new List<string>();
        }

        public static Assembly EntryAssembly => _entryAssembly.Value;
        public IList<string> Diagnostics { get; private set; }

        public bool Compile(ProjectContext context, string config)
        {
           // Mark ambient libraries as compiled
            if (_compiledLibraries.IsEmpty)
            {
                var libraries = DependencyContext.Default.CompileLibraries
                    .Where(x => x.Type == LibraryType.Project.ToString().ToLowerInvariant());

                foreach (var library in libraries)
                {
                    _compiledLibraries[library.Name] = true;
                }
            }

            bool compilationResult;

            // Check if already compiled
            if (_compiledLibraries.TryGetValue(context.RootProject.Identity.Name, out compilationResult))
            {
                return _compiledLibraries[context.RootProject.Identity.Name];
            }

           // Set up Output Paths
            var outputPaths = context.GetOutputPaths(config);
            var outputPath = outputPaths.CompilationOutputPath;
            var intermediateOutputPath = outputPaths.IntermediateOutputDirectoryPath;

            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(intermediateOutputPath);

            // Create the library exporter
            var exporter = context.CreateExporter(config);

            // Gather exports for the project
            var dependencies = exporter.GetDependencies().ToList();

            var diagnostics = new List<DiagnosticMessage>();

            // Collect dependency diagnostics
            foreach (var diag in context.LibraryManager.GetAllDiagnostics())
            {
                // Some library paths may not be resolved by the project model (e.g in production)
                // So, here we don't grab any diagnostics and we will search in probing folders 

                //Diagnostics.Add(diag.FormattedMessage);
                //diagnostics.Add(diag);
            }

            if (diagnostics.Any(d => d.Severity == DiagnosticMessageSeverity.Error))
            {
                // We got an unresolved dependency or missing framework. Don't continue the compilation.
                return _compiledLibraries[context.RootProject.Identity.Name] = false;
            }

            // Get compilation options
            var outputName = outputPaths.CompilationFiles.Assembly;
            var compilationOptions = context.ResolveCompilationOptions(config);

            // Set default platform if it isn't already set and we're on desktop
            if (compilationOptions.EmitEntryPoint == true && string.IsNullOrEmpty(compilationOptions.Platform) && context.TargetFramework.IsDesktop())
            {
                // See https://github.com/dotnet/cli/issues/2428 for more details.
                compilationOptions.Platform = RuntimeInformation.ProcessArchitecture == Architecture.X64 ?
                    "x64" : "anycpu32bitpreferred";
            }

            var references = new List<string>();
            var sourceFiles = new List<string>();

            // Add metadata options
            var assemblyInfoOptions = AssemblyInfoOptions.CreateForProject(context);

            // Get the runtime directory
            var runtimeDirectory = Path.GetDirectoryName(EntryAssembly.Location);

            foreach (var dependency in dependencies)
            {
                sourceFiles.AddRange(dependency.SourceReferences.Select(s => s.GetTransformedFile(intermediateOutputPath)));

                var library = dependency.Library as ProjectDescription;
                var package = dependency.Library as PackageDescription;

                // Compile other referenced libraries
                if (library != null  && dependency.CompilationAssemblies.Any())
                {
                    if (!_compiledLibraries.TryGetValue(library.Identity.Name, out compilationResult))
                    {
                        var projectContext = ProjectContext.CreateContextForEachFramework(library.Project.ProjectDirectory).FirstOrDefault();

                        if (projectContext != null)
                        {
                           // Right now, if !success we try to use the last build
                           var success = Compile(projectContext, config);
                        }
                    }
                }

                // Check if a project library is not resolved (e.g in production)
                if (library != null && !dependency.CompilationAssemblies.Any())
                {
                    var fileName = dependency.Library.Identity.Name + FileNameSuffixes.DotNet.DynamicLib;

                    // Search in the runtime directory for an ambient library
                    var path = Path.Combine(runtimeDirectory, fileName);

                    if (!File.Exists(path))
                    {
                        // Fallback to the extension probing folder
                        path = Path.Combine(context.ProjectDirectory, "lib", fileName);
                    }

                    references.Add(path);
                }
                // Check if a package library is not resolved (e.g in production)
                else if (package != null && !dependency.CompilationAssemblies.Any())
                {
                    foreach (var assembly in package.CompileTimeAssemblies)
                    {
                        var fileName = Path.GetFileName(assembly.Path);

                        // Search in the runtime directory
                        var path = Path.Combine(runtimeDirectory, fileName);

                        if (!File.Exists(path))
                        {
                            // Fallback to the "refs" subfolder
                            path = Path.Combine(runtimeDirectory, "refs", fileName);

                            if (!File.Exists(path))
                            {
                                // Fallback to the extension probing folder
                                path = Path.Combine(context.ProjectDirectory, "lib", fileName);
                            }
                        }

                        references.Add(path);
                    }
                }
                else
                {
                    references.AddRange(dependency.CompilationAssemblies.Select(r => r.ResolvedPath));
                }
            }

            // Check again if already compiled, here through the dependency graph
            if (_compiledLibraries.TryGetValue(context.RootProject.Identity.Name, out compilationResult))
            {
                return _compiledLibraries[context.RootProject.Identity.Name];
            }

            // Mark this library as compiled even if it will fail
            _compiledLibraries[context.RootProject.Identity.Name] = false;

            var resources = new List<string>();
            if (compilationOptions.PreserveCompilationContext == true)
            {
                var allExports = exporter.GetAllExports().ToList();
                var dependencyContext = new DependencyContextBuilder().Build(compilationOptions,
                    allExports,
                    allExports,
                    false, // For now, just assume non-portable mode in the legacy deps file (this is going away soon anyway)
                    context.TargetFramework,
                    context.RuntimeIdentifier ?? string.Empty);

                var writer = new DependencyContextWriter();
                var depsJsonFile = Path.Combine(intermediateOutputPath, compilationOptions.OutputName + "dotnet-compile.deps.json");
                using (var fileStream = File.Create(depsJsonFile))
                {
                    writer.Write(dependencyContext, fileStream);
                }

                resources.Add($"\"{depsJsonFile}\",{compilationOptions.OutputName}.deps.json");
            }

            // Add project source files
            if (compilationOptions.CompileInclude == null)
            {
                sourceFiles.AddRange(context.ProjectFile.Files.SourceFiles);
            }
            else {
                var includeFiles = IncludeFilesResolver.GetIncludeFiles(compilationOptions.CompileInclude, "/", diagnostics: null);
                sourceFiles.AddRange(includeFiles.Select(f => f.SourcePath));
            }

            if (String.IsNullOrEmpty(intermediateOutputPath))
            {
                return _compiledLibraries[context.RootProject.Identity.Name] = false;
            }

            var translated = TranslateCommonOptions(compilationOptions, outputName);

            var allArgs = new List<string>(translated);
            allArgs.AddRange(GetDefaultOptions());

            // Generate assembly info
            var assemblyInfo = Path.Combine(intermediateOutputPath, $"dotnet-compile.assemblyinfo.cs");

            allArgs.Add($"\"{assemblyInfo}\"");

            if (!String.IsNullOrEmpty(outputName))
            {
                allArgs.Add($"-out:\"{outputName}\"");
            }

            allArgs.AddRange(references.Select(r => $"-r:\"{r}\""));
            allArgs.AddRange(resources.Select(resource => $"-resource:{resource}"));
            allArgs.AddRange(sourceFiles.Select(s => $"\"{s}\""));

            // Gather all compile IO
            var inputs = new List<string>();
            var outputs = new List<string>();

            inputs.Add(context.ProjectFile.ProjectFilePath);
 
            if (context.LockFile != null)
            {
                inputs.Add(context.LockFile.LockFilePath);
            }

            if (context.LockFile.ExportFile != null)
            {
                inputs.Add(context.LockFile.ExportFile.ExportFilePath);
            }

            inputs.AddRange(sourceFiles);
            inputs.AddRange(references);
            outputs.AddRange(outputPaths.CompilationFiles.All());

            // Locate RSP file
            var rsp = Path.Combine(intermediateOutputPath, $"dotnet-compile-csc.rsp");

            // Check if there is no need to compile
            if (!CheckMissingIO(inputs, outputs) && !TimestampsChanged(inputs, outputs))
            {
                if (File.Exists(rsp))
                {
                    // Check if the compilation context has been changed
                    var prevInputs = new HashSet<string>(File.ReadAllLines(rsp));
                    var newInputs = new HashSet<string>(allArgs);

                    if (!prevInputs.Except(newInputs).Any() && ! newInputs.Except(prevInputs).Any())
                        return _compiledLibraries[context.RootProject.Identity.Name] = true;
                }
                else
                {
                    // Write RSP file for the next time
                    File.WriteAllLines(rsp, allArgs);
                    return _compiledLibraries[context.RootProject.Identity.Name] = true;
                }
            }

            // Write assembly info and RSP files
            File.WriteAllText(assemblyInfo, AssemblyInfoFileGenerator.GenerateCSharp(assemblyInfoOptions, sourceFiles));
            File.WriteAllLines(rsp, allArgs);

            // Locate runtime config files
            var runtimeConfigPath = Path.Combine(runtimeDirectory, EntryAssembly.GetName().Name + ".runtimeconfig.json");
            var cscRuntimeConfigPath =  Path.Combine(runtimeDirectory, "csc.runtimeconfig.json");

            // Automatically create the csc runtime config file
            if (File.Exists(runtimeConfigPath) && (!File.Exists(cscRuntimeConfigPath)
                || File.GetLastWriteTimeUtc(runtimeConfigPath) > File.GetLastWriteTimeUtc(cscRuntimeConfigPath)))
            {
                File.Copy(runtimeConfigPath, cscRuntimeConfigPath, true);
            }

            // Execute CSC!
            var result = Command.Create("csc.dll", new string[] { $"-noconfig", "@" + $"{rsp}" })
                .WorkingDirectory(runtimeDirectory)
                .OnErrorLine(line => Diagnostics.Add(line))
                .OnOutputLine(line => Diagnostics.Add(line))
                .Execute();

            return _compiledLibraries[context.RootProject.Identity.Name] = result.ExitCode == 0;
        }

        private static IEnumerable<string> GetDefaultOptions()
        {
            var args = new List<string>()
            {
                "-nostdlib",
                "-nologo",
            };

            return args;
        }

        private static IEnumerable<string> TranslateCommonOptions(CommonCompilerOptions options, string outputName)
        {
            List<string> commonArgs = new List<string>();

            if (options.Defines != null)
            {
                commonArgs.AddRange(options.Defines.Select(def => $"-d:{def}"));
            }

            if (options.SuppressWarnings != null)
            {
                commonArgs.AddRange(options.SuppressWarnings.Select(w => $"-nowarn:{w}"));
            }

            // Additional arguments are added verbatim
            if (options.AdditionalArguments != null)
            {
                commonArgs.AddRange(options.AdditionalArguments);
            }

            if (options.LanguageVersion != null)
            {
                commonArgs.Add($"-langversion:{GetLanguageVersion(options.LanguageVersion)}");
            }

            if (options.Platform != null)
            {
                commonArgs.Add($"-platform:{options.Platform}");
            }

            if (options.AllowUnsafe == true)
            {
                commonArgs.Add("-unsafe");
            }

            if (options.WarningsAsErrors == true)
            {
                commonArgs.Add("-warnaserror");
            }

            if (options.Optimize == true)
            {
                commonArgs.Add("-optimize");
            }

            if (options.KeyFile != null)
            {
                commonArgs.Add($"-keyfile:\"{options.KeyFile}\"");

                // If we're not on Windows, full signing isn't supported, so we'll
                // public sign, unless the public sign switch has been set to false
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    options.PublicSign == null)
                {
                    commonArgs.Add("-publicsign");
                }
            }

            if (options.DelaySign == true)
            {
                commonArgs.Add("-delaysign");
            }

            if (options.PublicSign == true)
            {
                commonArgs.Add("-publicsign");
            }

            if (options.GenerateXmlDocumentation == true)
            {
                commonArgs.Add($"-doc:\"{Path.ChangeExtension(outputName, "xml")}\"");
            }

            if (options.EmitEntryPoint != true)
            {
                commonArgs.Add("-t:library");
            }

            if (string.IsNullOrEmpty(options.DebugType))
            {
                commonArgs.Add(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "-debug:full"
                    : "-debug:portable");
            }
            else
            {
                commonArgs.Add(options.DebugType == "portable"
                    ? "-debug:portable"
                    : "-debug:full");
            }

            return commonArgs;
        }

        private static string GetLanguageVersion(string languageVersion)
        {
            // project.json supports the enum that the roslyn APIs expose
            if (languageVersion?.StartsWith("csharp", StringComparison.OrdinalIgnoreCase) == true)
            {
                // We'll be left with the number csharp6 = 6
                return languageVersion.Substring("csharp".Length);
            }
            return languageVersion;
        }

        private bool CheckMissingIO(IEnumerable<string> inputs, IEnumerable<string> outputs)
        {
            if (!inputs.Any() || !outputs.Any())
            {
                return false;
            }

            return CheckMissingIO(inputs) || CheckMissingIO(outputs);
        }

        private bool CheckMissingIO(IEnumerable<string> items)
        {
            return items.Where(i => !File.Exists(i)).Any();
        }

        private bool TimestampsChanged(IEnumerable<string> inputs, IEnumerable<string> outputs)
        {
            // Find the output with the earliest write time
            var minDateUtc = DateTime.MaxValue;

            foreach (var outputPath in outputs)
            {
                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(outputPath);

                if (lastWriteTimeUtc < minDateUtc)
                {
                    minDateUtc = lastWriteTimeUtc;
                }
            }

            // Find inputs that are newer than the earliest output
            return inputs.Any(p => File.GetLastWriteTimeUtc(p) >= minDateUtc);
        }
    }
}
