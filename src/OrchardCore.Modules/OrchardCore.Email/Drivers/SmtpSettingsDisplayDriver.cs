using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Email.Services;
using OrchardCore.Environment.Shell;
using OrchardCore.Settings;

namespace OrchardCore.Email.Drivers
{
    public class SmtpSettingsDisplayDriver : SectionDisplayDriver<ISite, SmtpSettings>
    {
        public const string GroupId = "email";
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public SmtpSettingsDisplayDriver(
            IDataProtectionProvider dataProtectionProvider,
            IShellHost shellHost,
            ShellSettings shellSettings,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(SmtpSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageEmailSettings))
            {
                return null;
            }

            var shapes = new List<IDisplayResult>
            {
                Initialize<SmtpSettings>("SmtpSettings_Edit", model =>
                {
                    model.DefaultSender = settings.DefaultSender;
                    model.DeliveryMethod = settings.DeliveryMethod;
                    model.PickupDirectoryLocation = settings.PickupDirectoryLocation;
                    model.Host = settings.Host;
                    model.Port = settings.Port;
                    model.EncryptionMethod = settings.EncryptionMethod;
                    model.AutoSelectEncryption = settings.AutoSelectEncryption;
                    model.RequireCredentials = settings.RequireCredentials;
                    model.UseDefaultCredentials = settings.UseDefaultCredentials;
                    model.UserName = settings.UserName;
                    model.Password = settings.Password;
                    model.PasswordSecret = settings.PasswordSecret;
                }).Location("Content:5").OnGroup(GroupId)
            };

            if (settings?.DefaultSender != null)
            {
                shapes.Add(Dynamic("SmtpSettings_TestButton").Location("Actions").OnGroup(GroupId));
            }

            return Combine(shapes);
        }

        public override async Task<IDisplayResult> UpdateAsync(SmtpSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageEmailSettings))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var previousPassword = settings.Password;
                await context.Updater.TryUpdateModelAsync(settings, Prefix);

                // Restore password if the input is empty, meaning that it has not been reset.
                if (string.IsNullOrWhiteSpace(settings.Password))
                {
                    settings.Password = previousPassword;
                }
                else
                {
                    // encrypt the password
                    var protector = _dataProtectionProvider.CreateProtector(nameof(SmtpSettingsConfiguration));
                    settings.Password = protector.Protect(settings.Password);
                }

                // Release the tenant to apply the settings
                await _shellHost.ReleaseShellContextAsync(_shellSettings);
            }

            return await EditAsync(settings, context);
        }
    }
}
