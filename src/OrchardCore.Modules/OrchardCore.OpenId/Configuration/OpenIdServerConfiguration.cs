using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Server.DataProtection;
using OrchardCore.Modules;
using OrchardCore.OpenId.Services;
using OrchardCore.OpenId.Settings;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OrchardCore.OpenId.Configuration
{
    [Feature(OpenIdConstants.Features.Server)]
    public class OpenIdServerConfiguration : IConfigureOptions<AuthenticationOptions>,
        IConfigureOptions<OpenIddictServerOptions>,
        IConfigureOptions<OpenIddictServerDataProtectionOptions>,
        IConfigureNamedOptions<OpenIddictServerAspNetCoreOptions>
    {
        private readonly ILogger _logger;
        private readonly IOpenIdServerService _serverService;

        public OpenIdServerConfiguration(
            ILogger<OpenIdServerConfiguration> logger,
            IOpenIdServerService serverService)
        {
            _logger = logger;
            _serverService = serverService;
        }

        public void Configure(AuthenticationOptions options)
        {
            var settings = GetServerSettingsAsync().GetAwaiter().GetResult();
            if (settings == null)
            {
                return;
            }

            options.AddScheme<OpenIddictServerAspNetCoreHandler>(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, displayName: null);
        }

        public void Configure(OpenIddictServerOptions options)
        {
            var settings = GetServerSettingsAsync().GetAwaiter().GetResult();
            if (settings == null)
            {
                return;
            }

            options.Issuer = settings.Authority;
            options.DisableAccessTokenEncryption = settings.DisableAccessTokenEncryption;
            options.DisableRollingRefreshTokens = settings.DisableRollingRefreshTokens;
            options.UseReferenceAccessTokens = settings.UseReferenceAccessTokens;

            foreach (var key in _serverService.GetEncryptionKeysAsync().GetAwaiter().GetResult())
            {
                options.EncryptionCredentials.Add(new EncryptingCredentials(key,
                    SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512));
            }

            foreach (var key in _serverService.GetSigningKeysAsync().GetAwaiter().GetResult())
            {
                options.SigningCredentials.Add(new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
            }

            if (settings.AuthorizationEndpointPath.HasValue)
            {
                options.AuthorizationEndpointUris.Add(new Uri(settings.AuthorizationEndpointPath.Value, UriKind.Relative));
            }
            if (settings.LogoutEndpointPath.HasValue)
            {
                options.LogoutEndpointUris.Add(new Uri(settings.LogoutEndpointPath.Value, UriKind.Relative));
            }
            if (settings.TokenEndpointPath.HasValue)
            {
                options.TokenEndpointUris.Add(new Uri(settings.TokenEndpointPath.Value, UriKind.Relative));
            }
            if (settings.UserinfoEndpointPath.HasValue)
            {
                options.UserinfoEndpointUris.Add(new Uri(settings.UserinfoEndpointPath.Value, UriKind.Relative));
            }

            // For now, response types and response modes are not directly
            // configurable and are inferred from the selected flows.
            if (settings.AllowAuthorizationCodeFlow)
            {
                options.CodeChallengeMethods.Add(CodeChallengeMethods.Sha256);

                options.GrantTypes.Add(GrantTypes.AuthorizationCode);

                options.ResponseModes.Add(ResponseModes.FormPost);
                options.ResponseModes.Add(ResponseModes.Fragment);
                options.ResponseModes.Add(ResponseModes.Query);

                options.ResponseTypes.Add(ResponseTypes.Code);
            }
            if (settings.AllowClientCredentialsFlow)
            {
                options.GrantTypes.Add(GrantTypes.ClientCredentials);
            }
            if (settings.AllowHybridFlow)
            {
                options.CodeChallengeMethods.Add(CodeChallengeMethods.Sha256);

                options.GrantTypes.Add(GrantTypes.AuthorizationCode);
                options.GrantTypes.Add(GrantTypes.Implicit);

                options.ResponseModes.Add(ResponseModes.FormPost);
                options.ResponseModes.Add(ResponseModes.Fragment);

                options.ResponseTypes.Add(ResponseTypes.Code + ' ' + ResponseTypes.IdToken);
                options.ResponseTypes.Add(ResponseTypes.Code + ' ' + ResponseTypes.IdToken + ' ' + ResponseTypes.Token);
                options.ResponseTypes.Add(ResponseTypes.Code + ' ' + ResponseTypes.Token);
            }
            if (settings.AllowImplicitFlow)
            {
                options.GrantTypes.Add(GrantTypes.Implicit);

                options.ResponseModes.Add(ResponseModes.FormPost);
                options.ResponseModes.Add(ResponseModes.Fragment);

                options.ResponseTypes.Add(ResponseTypes.IdToken);
                options.ResponseTypes.Add(ResponseTypes.IdToken + ' ' + ResponseTypes.Token);
                options.ResponseTypes.Add(ResponseTypes.Token);
            }
            if (settings.AllowPasswordFlow)
            {
                options.GrantTypes.Add(GrantTypes.Password);
            }
            if (settings.AllowRefreshTokenFlow)
            {
                options.GrantTypes.Add(GrantTypes.RefreshToken);

                options.Scopes.Add(Scopes.OfflineAccess);
            }

            options.Scopes.Add(Scopes.Email);
            options.Scopes.Add(Scopes.Phone);
            options.Scopes.Add(Scopes.Profile);
            options.Scopes.Add(Scopes.Roles);
        }

        public void Configure(OpenIddictServerDataProtectionOptions options)
        {
            var settings = GetServerSettingsAsync().GetAwaiter().GetResult();
            if (settings == null)
            {
                return;
            }

            // All the tokens produced by the server feature use ASP.NET Core Data Protection as the default
            // token format, but an option is provided to allow switching to JWT for access tokens only.
            options.PreferDefaultAccessTokenFormat = settings.AccessTokenFormat == OpenIdServerSettings.TokenFormat.JsonWebToken;
        }

        public void Configure(string name, OpenIddictServerAspNetCoreOptions options)
        {
            // Note: the OpenID module handles the authorization, logout, token and userinfo requests
            // in its dedicated ASP.NET Core MVC controller, which requires enabling the pass-through mode.
            options.EnableAuthorizationEndpointPassthrough = true;
            options.EnableLogoutEndpointPassthrough = true;
            options.EnableTokenEndpointPassthrough = true;
            options.EnableUserinfoEndpointPassthrough = true;

            // Note: caching is enabled for both authorization and logout requests to allow sending
            // large POST authorization and logout requests, but can be programmatically disabled, as the
            // authorization and logout views support flowing the entire payload and not just the request_id.
            options.EnableAuthorizationRequestCaching = true;
            options.EnableLogoutRequestCaching = true;

            // Note: error pass-through is enabled to allow the actions of the MVC authorization controller
            // to handle the errors returned by the interactive endpoints without relying on the generic
            // status code pages middleware to rewrite the response later in the request processing.
            options.EnableErrorPassthrough = true;

            // Note: in Orchard, transport security is usually configured via the dedicated HTTPS module.
            // To make configuration easier and avoid having to configure it in two different features,
            // the transport security requirement enforced by OpenIddict by default is always turned off.
            options.DisableTransportSecurityRequirement = true;
        }

        public void Configure(OpenIddictServerAspNetCoreOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");

        private async Task<OpenIdServerSettings> GetServerSettingsAsync()
        {
            var settings = await _serverService.GetSettingsAsync();
            if ((await _serverService.ValidateSettingsAsync(settings)).Any(result => result != ValidationResult.Success))
            {
                _logger.LogWarning("The OpenID Connect module is not correctly configured.");

                return null;
            }

            return settings;
        }
    }
}
