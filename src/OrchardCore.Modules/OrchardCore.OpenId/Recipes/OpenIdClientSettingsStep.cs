using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using OrchardCore.OpenId.Services;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.OpenId.Recipes
{
    /// <summary>
    /// This recipe step sets general OpenID Connect Client settings.
    /// </summary>
    public class OpenIdClientSettingsStep : IRecipeStepHandler
    {
        private readonly IOpenIdClientService _clientService;

        public OpenIdClientSettingsStep(IOpenIdClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, "OpenIdClientSettings", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step.ToObject<OpenIdClientSettingsStepModel>();
            var settings = await _clientService.LoadSettingsAsync();

            settings.Scopes = model.Scopes.Split(' ', ',');
            settings.Authority = !string.IsNullOrEmpty(model.Authority) ? new Uri(model.Authority, UriKind.Absolute) : null;
            settings.CallbackPath = model.CallbackPath;
            settings.ClientId = model.ClientId;
            settings.ClientSecret = model.ClientSecret;
            settings.DisplayName = model.DisplayName;
            settings.ResponseMode = model.ResponseMode;
            settings.ResponseType = model.ResponseType;
            settings.SignedOutCallbackPath = model.SignedOutCallbackPath;
            settings.SignedOutRedirectUri = model.SignedOutRedirectUri;

            await _clientService.UpdateSettingsAsync(settings);
        }
    }
}
