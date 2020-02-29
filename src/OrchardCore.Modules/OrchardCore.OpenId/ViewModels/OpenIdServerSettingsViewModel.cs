using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using static OrchardCore.OpenId.Settings.OpenIdServerSettings;

namespace OrchardCore.OpenId.ViewModels
{
    public class OpenIdServerSettingsViewModel
    {
        public TokenFormat AccessTokenFormat { get; set; }

        [Url]
        public string Authority { get; set; }

        public StoreLocation? CertificateStoreLocation { get; set; }
        public StoreName? CertificateStoreName { get; set; }
        public string CertificateThumbprint { get; set; }
        public IList<CertificateInfo> AvailableCertificates { get; } = new List<CertificateInfo>();
        public bool EnableTokenEndpoint { get; set; }
        public bool EnableAuthorizationEndpoint { get; set; }
        public bool EnableLogoutEndpoint { get; set; }
        public bool EnableUserInfoEndpoint { get; set; }
        public bool AllowPasswordFlow { get; set; }
        public bool AllowClientCredentialsFlow { get; set; }
        public bool AllowAuthorizationCodeFlow { get; set; }
        public bool AllowRefreshTokenFlow { get; set; }
        public bool AllowImplicitFlow { get; set; }
        public bool UseRollingTokens { get; set; }
        public bool IncludePermissionsInToken { get; set; }
        public bool IncludeRolesInToken { get; set; }
        public bool IncludeLegacyMicrosoftClaimsInToken { get; set; }

        public class CertificateInfo
        {
            public string FriendlyName { get; set; }
            public string Issuer { get; set; }
            public DateTime NotAfter { get; set; }
            public DateTime NotBefore { get; set; }
            public StoreLocation StoreLocation { get; set; }
            public StoreName StoreName { get; set; }
            public string Subject { get; set; }
            public string ThumbPrint { get; set; }
            public bool HasPrivateKey { get; set; }
            public bool Archived { get; set; }
        }
    }
}
