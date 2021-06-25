using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.DataLocalization.Models;
using OrchardCore.DataLocalization.Services;

namespace OrchardCore.DataLocalization.Recipes
{
    public class TranslationsStep : IRecipeStepHandler
    {
        private readonly TranslationsManager _translationsManager;

        public TranslationsStep(TranslationsManager translationsManager)
        {
            _translationsManager = translationsManager;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "DynamicDataTranslations", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.Step.Property("DynamicDataTranslations").Value is JObject translations)
            {
                foreach (var property in translations.Properties())
                {
                    var name = property.Name;
                    var value = property.Value.ToObject<Translation>();

                    await _translationsManager.UpdateTranslationAsync(name, value);
                }
            }
        }
    }
}
