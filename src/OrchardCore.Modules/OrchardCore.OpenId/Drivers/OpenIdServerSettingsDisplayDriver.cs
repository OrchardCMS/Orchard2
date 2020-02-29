using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.OpenId.Services;
using OrchardCore.OpenId.Settings;
using OrchardCore.OpenId.ViewModels;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OrchardCore.OpenId.ViewModels.OpenIdServerSettingsViewModel;

namespace OrchardCore.OpenId.Drivers
{
    public class OpenIdServerSettingsDisplayDriver : DisplayDriver<OpenIdServerSettings>
    {
        private readonly IOpenIdServerService _serverService;

        public OpenIdServerSettingsDisplayDriver(IOpenIdServerService serverService)
            => _serverService = serverService;

        public override Task<IDisplayResult> EditAsync(OpenIdServerSettings settings, BuildEditorContext context)
            => Task.FromResult<IDisplayResult>(Initialize<OpenIdServerSettingsViewModel>("OpenIdServerSettings_Edit", async model =>
            {
                model.AccessTokenFormat = settings.AccessTokenFormat;
                model.Authority = settings.Authority?.AbsoluteUri;

                model.CertificateStoreLocation = settings.CertificateStoreLocation;
                model.CertificateStoreName = settings.CertificateStoreName;
                model.CertificateThumbprint = settings.CertificateThumbprint;

                model.EnableAuthorizationEndpoint = settings.AuthorizationEndpointPath.HasValue;
                model.EnableLogoutEndpoint = settings.LogoutEndpointPath.HasValue;
                model.EnableTokenEndpoint = settings.TokenEndpointPath.HasValue;
                model.EnableUserInfoEndpoint = settings.UserinfoEndpointPath.HasValue;

                model.AllowPasswordFlow = settings.GrantTypes.Contains(GrantTypes.Password);
                model.AllowClientCredentialsFlow = settings.GrantTypes.Contains(GrantTypes.ClientCredentials);
                model.AllowAuthorizationCodeFlow = settings.GrantTypes.Contains(GrantTypes.AuthorizationCode);
                model.AllowRefreshTokenFlow = settings.GrantTypes.Contains(GrantTypes.RefreshToken);
                model.AllowImplicitFlow = settings.GrantTypes.Contains(GrantTypes.Implicit);

                model.IncludePermissionsInToken = settings.IncludePermissionsInToken;
                model.IncludeRolesInToken = settings.IncludeRolesInToken;
                model.IncludeLegacyMicrosoftClaimsInToken = settings.IncludeLegacyMicrosoftClaimsInToken;

                model.UseRollingTokens = settings.UseRollingTokens;

                foreach (var (certificate, location, name) in await _serverService.GetAvailableCertificatesAsync())
                {
                    model.AvailableCertificates.Add(new CertificateInfo
                    {
                        StoreLocation = location,
                        StoreName = name,
                        FriendlyName = certificate.FriendlyName,
                        Issuer = certificate.Issuer,
                        Subject = certificate.Subject,
                        NotBefore = certificate.NotBefore,
                        NotAfter = certificate.NotAfter,
                        ThumbPrint = certificate.Thumbprint,
                        HasPrivateKey = certificate.HasPrivateKey,
                        Archived = certificate.Archived
                    });
                }
            }).Location("Content:2"));

        public override async Task<IDisplayResult> UpdateAsync(OpenIdServerSettings settings, UpdateEditorContext context)
        {
            var model = new OpenIdServerSettingsViewModel();
            await context.Updater.TryUpdateModelAsync(model, Prefix);

            settings.AccessTokenFormat = model.AccessTokenFormat;
            settings.Authority = !string.IsNullOrEmpty(model.Authority) ? new Uri(model.Authority, UriKind.Absolute) : null;

            settings.CertificateStoreLocation = model.CertificateStoreLocation;
            settings.CertificateStoreName = model.CertificateStoreName;
            settings.CertificateThumbprint = model.CertificateThumbprint;

            settings.AuthorizationEndpointPath = model.EnableAuthorizationEndpoint ?
                new PathString("/connect/authorize") : PathString.Empty;
            settings.LogoutEndpointPath = model.EnableLogoutEndpoint ?
                new PathString("/connect/logout") : PathString.Empty;
            settings.TokenEndpointPath = model.EnableTokenEndpoint ?
                new PathString("/connect/token") : PathString.Empty;
            settings.UserinfoEndpointPath = model.EnableUserInfoEndpoint ?
                new PathString("/connect/userinfo") : PathString.Empty;

            if (model.AllowAuthorizationCodeFlow)
            {
                settings.GrantTypes.Add(GrantTypes.AuthorizationCode);
            }
            else
            {
                settings.GrantTypes.Remove(GrantTypes.AuthorizationCode);
            }

            if (model.AllowImplicitFlow)
            {
                settings.GrantTypes.Add(GrantTypes.Implicit);
            }
            else
            {
                settings.GrantTypes.Remove(GrantTypes.Implicit);
            }

            if (model.AllowClientCredentialsFlow)
            {
                settings.GrantTypes.Add(GrantTypes.ClientCredentials);
            }
            else
            {
                settings.GrantTypes.Remove(GrantTypes.ClientCredentials);
            }

            if (model.AllowPasswordFlow)
            {
                settings.GrantTypes.Add(GrantTypes.Password);
            }
            else
            {
                settings.GrantTypes.Remove(GrantTypes.Password);
            }

            if (model.AllowRefreshTokenFlow)
            {
                settings.GrantTypes.Add(GrantTypes.RefreshToken);
            }
            else
            {
                settings.GrantTypes.Remove(GrantTypes.RefreshToken);
            }

            settings.UseRollingTokens = model.UseRollingTokens;

            return await EditAsync(settings, context);
        }
    }
}
