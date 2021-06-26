using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OrchardCore.Deployment;
using OrchardCore.Email.Services;
using OrchardCore.Entities;
using OrchardCore.Settings;

namespace OrchardCore.Email.Deployment
{
    public class SmtpSettingsDeploymentSource : IDeploymentSource
    {
        private readonly ISiteService _siteService;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private ILogger _logger;

        public SmtpSettingsDeploymentSource(ISiteService siteService,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<SmtpSettingsDeploymentSource> logger)
        {
            _siteService = siteService;
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public async Task ProcessDeploymentStepAsync(DeploymentStep step, DeploymentPlanResult result)
        {
            var smtpSettingsStep = step as SmtpSettingsDeploymentStep;

            if (smtpSettingsStep == null)
            {
                return;
            }

            var smtpSettings = (await _siteService.GetSiteSettingsAsync()).As<SmtpSettings>();

            var jObject = JObject.FromObject(smtpSettings);

            jObject.Remove("Password");

            if (smtpSettingsStep.Password != DeploymentSecretHandler.Ignored)
            {
                // Default to cover error handling if protector is unsuccessful.
                var passwordRecipeSecret = new RecipeSecret();

                // The deployment step secret is added to the deployment step itself.
                var passwordDeploymentStepSecret = new DeploymentStepSecret("smtpPassword");

                switch (smtpSettingsStep.Password)
                {
                    case DeploymentSecretHandler.Encrypted:
                        passwordRecipeSecret = new RecipeSecret
                        {
                            Handler = RecipeSecretHandler.Encrypted,
                            Value = smtpSettings.Password
                        };
                        break;
                    case DeploymentSecretHandler.PlainText:
                        if (!String.IsNullOrWhiteSpace(smtpSettings.Password))
                        {
                            try
                            {
                                var protector = _dataProtectionProvider.CreateProtector(nameof(SmtpSettingsConfiguration));
                                passwordRecipeSecret = new RecipeSecret
                                {
                                    Handler = RecipeSecretHandler.PlainText,
                                    Value = protector.Unprotect(smtpSettings.Password)
                                };
                            }
                            catch
                            {
                                _logger.LogError("The Smtp password could not be decrypted. It may have been encrypted using a different key.");
                            }
                        }
                        break;
                    default:
                        break;

                };

                // Add the deployment secret to the `SmtpSettings` step section.
                jObject.Add(new JProperty("PasswordSecret", JObject.FromObject(passwordDeploymentStepSecret)));

                // Add the secrets value to the 'secrets' section in the recipe.
                result.Secrets["smtpPassword"] = JObject.FromObject(passwordRecipeSecret);
            }

            // Adding Smtp settings
            result.Steps.Add(new JObject(
                new JProperty("name", "SmtpSettings"),
                new JProperty("SmtpSettings", jObject)
            ));
        }
    }
}
