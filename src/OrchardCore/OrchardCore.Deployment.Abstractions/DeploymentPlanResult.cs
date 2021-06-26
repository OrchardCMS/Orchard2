using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Recipes.Models;

namespace OrchardCore.Deployment
{
    /// <summary>
    /// The state of a deployment plan built by sources.
    /// </summary>
    public class DeploymentPlanResult
    {
        public DeploymentPlanResult(IFileBuilder fileBuilder, RecipeDescriptor recipeDescriptor)
        {
            FileBuilder = fileBuilder;

            Recipe = new JObject();
            Recipe["name"] = recipeDescriptor.Name ?? "";
            Recipe["displayName"] = recipeDescriptor.DisplayName ?? "";
            Recipe["description"] = recipeDescriptor.Description ?? "";
            Recipe["author"] = recipeDescriptor.Author ?? "";
            Recipe["website"] = recipeDescriptor.WebSite ?? "";
            Recipe["version"] = recipeDescriptor.Version ?? "";
            Recipe["issetuprecipe"] = recipeDescriptor.IsSetupRecipe;
            Recipe["categories"] = new JArray(recipeDescriptor.Categories ?? new string[] { });
            Recipe["tags"] = new JArray(recipeDescriptor.Tags ?? new string[] { });
        }

        public JObject Recipe { get; }
        public JObject Secrets { get; } = new JObject();
        public IList<JObject> Steps { get; } = new List<JObject>();
        public IFileBuilder FileBuilder { get; }
        public async Task FinalizeAsync()
        {
            Recipe["secrets"] = Secrets;
            Recipe["steps"] = new JArray(Steps);

            // Add the recipe steps as its own file content
            await FileBuilder.SetFileAsync("Recipe.json", Encoding.UTF8.GetBytes(Recipe.ToString(Formatting.Indented)));
        }
    }
}
