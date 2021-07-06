using System;
using System.Threading.Tasks;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.ContentTypes.RecipeSteps
{
    /// <summary>
    /// This recipe step deletes content definition records.
    /// </summary>
    public class DeleteContentDefinitionStep : IRecipeStepHandler
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public DeleteContentDefinitionStep(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }

        public Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "DeleteContentDefinition", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var step = context.Step.ToObject<DeleteContentDefinitionStepModel>();

            foreach (var contentType in step.ContentTypes)
            {
                // The content definition manager tests existence before trying to delete.
                _contentDefinitionManager.DeleteTypeDefinition(contentType);
            }

            foreach (var contentPart in step.ContentParts)
            {
                // The content definition manager tests existence before trying to delete.
                _contentDefinitionManager.DeletePartDefinition(contentPart);
            }

            return Task.CompletedTask;
        }

        private class DeleteContentDefinitionStepModel
        {
            public string[] ContentTypes { get; set; } = Array.Empty<string>();
            public string[] ContentParts { get; set; } = Array.Empty<string>();
        }
    }
}
