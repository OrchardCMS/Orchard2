using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.Contents.Recipes
{
    /// <summary>
    /// This recipe step creates a set of content items.
    /// </summary>
    public class ContentStep : IRecipeStepHandler
    {
        public Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "Content", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var model = context.Step.ToObject<ContentStepModel>();

            var contentItems = model.Data.ToObject<ContentItem[]>();

            // We defer the import of content items to ensure that all required migrations are executed before,
            // e.g. this prevents a workflow triggered by an handler to be executed before worflows migrations.
            ShellScope.AddDeferredTask(scope =>
            {
                var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
                return contentManager.ImportAsync(contentItems);
            });

            return Task.CompletedTask;
        }
    }

    public class ContentStepModel
    {
        public JArray Data { get; set; }
    }
}
