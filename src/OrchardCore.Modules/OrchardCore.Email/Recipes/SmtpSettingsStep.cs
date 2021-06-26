using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OrchardCore.Deployment;
using OrchardCore.Email.Services;
using OrchardCore.Entities;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.Settings;

namespace OrchardCore.Email.Recipes
{
    public class SmtpSettingsStep : IRecipeStepHandler
    {
        private readonly ISiteService _siteService;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private ILogger _logger;

        public SmtpSettingsStep(ISiteService siteService,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<SmtpSettingsStep> logger
            )
        {
            _siteService = siteService;
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, "SmtpSettings", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var jStepSettings = context.Step["SmtpSettings"] as JObject;
            if (jStepSettings == null)
            {
                return;
            }


            var passwordSecret = jStepSettings["PasswordSecret"]?.ToObject<RecipeSecret>();

            string password = String.Empty;
            if (passwordSecret != null)
            {
                // Remove the secret from the step
                jStepSettings.Remove("PasswordSecret");

                switch (passwordSecret.Handler)
                {
                    case RecipeSecretHandler.PlainText:
                        if (!String.IsNullOrEmpty(passwordSecret.Value))
                        {
                            // Encrypt the password as it was supplied in plain text by the user.
                            password = EncryptPassword(passwordSecret.Value);
                        }
                        else
                        {
                            _logger.LogError("Password secret not provided for 'SmtpSettings'");
                        }
                        break;
                    case RecipeSecretHandler.Encrypted:
                        if (!String.IsNullOrEmpty(passwordSecret.Value))
                        {
                            try
                            {
                                var protector = _dataProtectionProvider.CreateProtector(nameof(SmtpSettingsConfiguration));
                                // Decrypt the password to test if the encryption keys are valid.
                                protector.Unprotect(passwordSecret.Value);
                                // On success we can store the supplied value.
                                password = passwordSecret.Value;
                            }
                            catch
                            {
                                _logger.LogError("The Smtp password could not be decrypted. It may have been encrypted using a different key.");
                            }
                        }
                        break;
                    default:
                        break;
                }

            }

            var site = await _siteService.LoadSiteSettingsAsync();
            // Retrieve the curent site settings first and merge them with the step values so we don't replace an ignored value.
            var jSiteSettings = site.Properties["SmtpSettings"] as JObject;

            if (jSiteSettings != null)
            {
                jStepSettings.Merge(jSiteSettings);
            }

            var model = jStepSettings.ToObject<SmtpSettings>();

            if (!String.IsNullOrEmpty(password))
            {
                model.Password = password;
            }

            site.Put(model);
            await _siteService.UpdateSiteSettingsAsync(site);
        }

        private string EncryptPassword(string password)
        {
            var protector = _dataProtectionProvider.CreateProtector(nameof(SmtpSettingsConfiguration));
            return protector.Protect(password);
        }
    }
}
