using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Recipes.Models
{
    public class RecipeExecutionContext
    {
        public string ExecutionId { get; set; }
        public object Environment { get; set; }
        public string Name { get; set; }
        public JObject Secrets { get; set; }
        public JObject Step { get; set; }
        public RecipeDescriptor RecipeDescriptor { get; set; }
        public IEnumerable<RecipeDescriptor> InnerRecipes { get; set; }
    }
}
