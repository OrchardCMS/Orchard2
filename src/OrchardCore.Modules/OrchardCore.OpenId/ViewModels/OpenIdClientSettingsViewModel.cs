using System.ComponentModel.DataAnnotations;

namespace OrchardCore.OpenId.ViewModels
{
    public class OpenIdClientSettingsViewModel
    {
        [Required]
        public string DisplayName { get; set; }
        public bool TestingModeEnabled { get; set; }
        [Required(ErrorMessage = "Authority is required")]
        public string Authority { get; set; }
        [Required(ErrorMessage = "ClientId is required")]
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        [RegularExpression(@"\/[-A-Za-z0-9+&@#\/%?=~_|!:,.;]+[-A-Za-z0-9+&@#\/%=~_|]", ErrorMessage = "Invalid path")]
        public string CallbackPath { get; set; }
        [Url(ErrorMessage = "Invalid signeout redirect url")]
        public string SignedOutRedirectUri { get; set; }
        [RegularExpression(@"\/[-A-Za-z0-9+&@#\/%?=~_|!:,.;]+[-A-Za-z0-9+&@#\/%=~_|]", ErrorMessage="Invalid path")]
        public string SignedOutCallbackPath { get; set; }
        public string AllowedScopes { get; set; }
    }
}
