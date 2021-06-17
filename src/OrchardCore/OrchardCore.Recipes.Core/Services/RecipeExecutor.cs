using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;
using OrchardCore.Recipes.Events;
using OrchardCore.Recipes.Models;
using OrchardCore.Scripting;

namespace OrchardCore.Recipes.Services
{
    public class RecipeExecutor : IRecipeExecutor
    {
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly IEnumerable<IRecipeEventHandler> _recipeEventHandlers;
        private readonly ILogger _logger;

        private readonly Dictionary<string, List<IGlobalMethodProvider>> _methodProviders = new Dictionary<string, List<IGlobalMethodProvider>>();

        public RecipeExecutor(
            IShellHost shellHost,
            ShellSettings shellSettings,
            IEnumerable<IRecipeEventHandler> recipeEventHandlers,
            ILogger<RecipeExecutor> logger)
        {
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _recipeEventHandlers = recipeEventHandlers;
            _logger = logger;
        }

        public async Task<string> ExecuteAsync(string executionId, RecipeDescriptor recipeDescriptor, IDictionary<string, object> environment, CancellationToken cancellationToken)
        {
            await _recipeEventHandlers.InvokeAsync((handler, executionId, recipeDescriptor) => handler.RecipeExecutingAsync(executionId, recipeDescriptor), executionId, recipeDescriptor, _logger);

            try
            {
                var methodProviders = new List<IGlobalMethodProvider>();
                _methodProviders.Add(executionId, methodProviders);

                methodProviders.Add(new ParametersMethodProvider(environment));
                methodProviders.Add(new ConfigurationMethodProvider(_shellSettings.ShellConfiguration));

                var result = new RecipeResult { ExecutionId = executionId };

                using (var stream = recipeDescriptor.RecipeFileInfo.CreateReadStream())
                {
                    using var file = new StreamReader(stream);
                    using var reader = new JsonTextReader(file);

                    // Go to Steps, then iterate.
                    while (await reader.ReadAsync())
                    {
                        if (reader.Path == "variables")
                        {
                            await reader.ReadAsync();

                            var variables = await JObject.LoadAsync(reader);

                            methodProviders.Add(new VariablesMethodProvider(variables, methodProviders));
                        }

                        if (reader.Path == "steps" && reader.TokenType == JsonToken.StartArray)
                        {
                            while (await reader.ReadAsync() && reader.Depth > 1)
                            {
                                if (reader.Depth == 2)
                                {
                                    var child = await JObject.LoadAsync(reader);

                                    var recipeStep = new RecipeExecutionContext
                                    {
                                        Name = child.Value<string>("name"),
                                        Step = child,
                                        ExecutionId = executionId,
                                        Environment = environment,
                                        RecipeDescriptor = recipeDescriptor
                                    };

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        _logger.LogError("Recipe interrupted by cancellation token.");
                                        return null;
                                    }

                                    var stepResult = new RecipeStepResult { StepName = recipeStep.Name };
                                    result.Steps.Add(stepResult);

                                    ExceptionDispatchInfo capturedException = null;
                                    try
                                    {
                                        await ExecuteStepAsync(recipeStep);
                                        stepResult.IsSuccessful = true;
                                    }
                                    catch (Exception e)
                                    {
                                        stepResult.IsSuccessful = false;
                                        stepResult.ErrorMessage = e.ToString();

                                        // Because we can't do some async processing the in catch or finally
                                        // blocks, we store the exception to throw it later.

                                        capturedException = ExceptionDispatchInfo.Capture(e);
                                    }

                                    stepResult.IsCompleted = true;

                                    if (stepResult.IsSuccessful == false)
                                    {
                                        capturedException.Throw();
                                    }

                                    if (recipeStep.InnerRecipes != null)
                                    {
                                        foreach (var descriptor in recipeStep.InnerRecipes)
                                        {
                                            var innerExecutionId = Guid.NewGuid().ToString();
                                            await ExecuteAsync(innerExecutionId, descriptor, environment, cancellationToken);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                await _recipeEventHandlers.InvokeAsync((handler, executionId, recipeDescriptor) => handler.RecipeExecutedAsync(executionId, recipeDescriptor), executionId, recipeDescriptor, _logger);

                return executionId;
            }
            catch (Exception)
            {
                await _recipeEventHandlers.InvokeAsync((handler, executionId, recipeDescriptor) => handler.ExecutionFailedAsync(executionId, recipeDescriptor), executionId, recipeDescriptor, _logger);

                throw;
            }
            finally
            {
                _methodProviders.Remove(executionId);
            }
        }

        private async Task ExecuteStepAsync(RecipeExecutionContext recipeStep)
        {
            var shellScope = recipeStep.RecipeDescriptor.RequireNewScope
                ? await _shellHost.GetScopeAsync(_shellSettings)
                : ShellScope.Current;

            await shellScope.UsingAsync(async scope =>
            {
                var recipeStepHandlers = scope.ServiceProvider.GetServices<IRecipeStepHandler>();
                var scriptingManager = scope.ServiceProvider.GetRequiredService<IScriptingManager>();

                // Substitutes the script elements by their actual values
                await EvaluateJsonTree(scriptingManager, recipeStep, recipeStep.Step);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Executing recipe step '{RecipeName}'.", recipeStep.Name);
                }

                await _recipeEventHandlers.InvokeAsync((handler, recipeStep) => handler.RecipeStepExecutingAsync(recipeStep), recipeStep, _logger);

                foreach (var recipeStepHandler in recipeStepHandlers)
                {
                    await recipeStepHandler.ExecuteAsync(recipeStep);
                }

                await _recipeEventHandlers.InvokeAsync((handler, recipeStep) => handler.RecipeStepExecutedAsync(recipeStep), recipeStep, _logger);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Finished executing recipe step '{RecipeName}'.", recipeStep.Name);
                }
            });
        }

        /// <summary>
        /// Traverse all the nodes of the json document and replaces their value if they are scripted.
        /// </summary>
        private async Task EvaluateJsonTree(IScriptingManager scriptingManager, RecipeExecutionContext context, JToken node)
        {
            switch (node.Type)
            {
                case JTokenType.Array:
                    var array = (JArray)node;
                    for (var i = 0; i < array.Count; i++)
                    {
                        await EvaluateJsonTree(scriptingManager, context, array[i]);
                    }

                    break;
                case JTokenType.Object:
                    foreach (var property in (JObject)node)
                    {
                       await EvaluateJsonTree(scriptingManager, context, property.Value);
                    }

                    break;
                case JTokenType.String:
                    const char scriptSeparator = ':';
                    var value = node.Value<string>();

                    // Evaluate the expression while the result is another expression
                    while (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        var scriptSeparatorIndex = value.IndexOf(scriptSeparator);
                        // Only remove brackets if this is a valid script expression, e.g. '[js:xxx]', or '[file:xxx]'
                        if (!(scriptSeparatorIndex > -1 && value[1..scriptSeparatorIndex].All(c => Char.IsLetter(c))))
                        {
                            break;
                        }

                        value = value.Trim('[', ']');

                        value = (await scriptingManager.EvaluateAsync(
                            value,
                            context.RecipeDescriptor.FileProvider,
                            context.RecipeDescriptor.BasePath,
                            _methodProviders[context.ExecutionId])
                            ?? "").ToString();

                        ((JValue)node).Value = value;
                    }

                    break;
            }
        }
    }
}
