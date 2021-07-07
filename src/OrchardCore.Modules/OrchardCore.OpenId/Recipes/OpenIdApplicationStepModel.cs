using System;
using System.Collections.Generic;
using System.Text;

namespace OrchardCore.OpenId.Recipes
{
    public class OpenIdApplicationStepModel
    {
        public string DisplayName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ConsentType { get; set; }
        public string Type { get; set; }
        public bool AllowAuthorizationCodeFlow { get; set; }
        public bool AllowClientCredentialsFlow { get; set; }
        public bool AllowImplicitFlow { get; set; }
        public bool AllowRefreshTokenFlow { get; set; }
        public bool AllowHybridFlow { get; set; }
        public bool AllowPasswordFlow { get; set; }
        public bool AllowLogoutEndpoint { get; set; }
        public string[] RedirectUris { get; set; }
        public string[] PostLogoutRedirectUris { get; set; }
        public string[] Roles { get; set; }
    }
}
