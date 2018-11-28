using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Microsoft.Authentication.Settings;
using OrchardCore.Microsoft.Authentication.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace OrchardCore.Microsoft.Authentication.Configuration
{
    [Feature(MicrosoftAuthenticationConstants.Features.AAD)]
    public class AzureADConfiguration :
        IConfigureOptions<AuthenticationOptions>,
        IConfigureNamedOptions<AzureADOptions>
    {
        private readonly IAzureADService _loginService;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<AzureADConfiguration> _logger;

        public AzureADConfiguration(
            IAzureADService loginService,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<AzureADConfiguration> logger)
        {
            _loginService = loginService;
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public void Configure(AuthenticationOptions options)
        {
            var loginSettings = GetAzureADSettingsAsync().GetAwaiter().GetResult();
            if (loginSettings == null)
            {
                return;
            }

            // Register the OpenID Connect client handler in the authentication handlers collection.
            options.AddScheme(AzureADDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "AzureAD";
                builder.HandlerType = typeof(OpenIdConnectHandler);
            });
        }

        public void Configure(string name, AzureADOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (!string.Equals(name, AzureADDefaults.AuthenticationScheme, StringComparison.Ordinal))
            {
                return;
            }

            var loginSettings = GetAzureADSettingsAsync().GetAwaiter().GetResult();
            if (loginSettings == null)
            {
                return;
            }
            options.ClientId = loginSettings.AppId;
            options.TenantId = loginSettings.TenantId;

            try
            {
                if (!string.IsNullOrWhiteSpace(loginSettings.AppSecret))
                    options.ClientSecret = _dataProtectionProvider.CreateProtector(MicrosoftAuthenticationConstants.Features.AAD).Unprotect(loginSettings.AppSecret);
            }
            catch
            {
                _logger.LogError("The Facebook secret keycould not be decrypted. It may have been encrypted using a different key.");
            }

            if (loginSettings.CallbackPath.HasValue)
            {
                options.CallbackPath = loginSettings.CallbackPath;
            }
        }

        public void Configure(AzureADOptions options) => Debug.Fail("This infrastructure method shouldn't be called.");

        private async Task<AzureADSettings> GetAzureADSettingsAsync()
        {
            var settings = await _loginService.GetSettingsAsync();
            if (_loginService.ValidateSettings(settings).Any(result => result != ValidationResult.Success))
            {
                _logger.LogWarning("The AzureAD Authentication is not correctly configured.");
                return null;
            }
            return settings;
        }

    }
}
