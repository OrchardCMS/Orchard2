using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.Secrets.Recipes
{
    public class SecretsRecipeStep : IRecipeStepHandler
    {
        private readonly ISecretCoordinator _secretCoordinator;
        private readonly IEnumerable<ISecretFactory> _factories;

        public SecretsRecipeStep(
            ISecretCoordinator secretCoordinator,
            IEnumerable<ISecretFactory> factories)
        {
            _secretCoordinator = secretCoordinator;
            _factories = factories;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "Secrets", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step;

            var secrets = ((JObject)context.Step["Secrets"]);
            foreach (var kvp in secrets)
            {
                var secretBinding = kvp.Value["SecretBinding"].ToObject<SecretBinding>();

                var secret = _factories.FirstOrDefault(x => x.Name == secretBinding.Type)?.Create();
                // This will always be plaintext as decrypt has already operated on the secret.
                var plaintext = kvp.Value["Secret"]?.ToString();
                if (!String.IsNullOrEmpty(plaintext))
                {
                    // Rehyrdate from plaintext to secret type.
                    secret = JsonConvert.DeserializeObject(plaintext, secret.GetType()) as Secret;
                }

                await _secretCoordinator.RemoveSecretAsync(kvp.Key, secretBinding.Store);
                await _secretCoordinator.UpdateSecretAsync(kvp.Key, secretBinding, secret);
            }
        }
    }
}
