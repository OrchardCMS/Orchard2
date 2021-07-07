using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrchardCore.OpenId.Recipes
{
    public class OpenIdClientSettingsStepModel
    {
        public string DisplayName { get; set; }

        [Url]
        public string Authority { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CallbackPath { get; set; }
        public string SignedOutRedirectUri { get; set; }
        public string SignedOutCallbackPath { get; set; }
        public string Scopes { get; set; }
        public string ResponseType { get; set; }
        public string ResponseMode { get; set; }
    }
}
