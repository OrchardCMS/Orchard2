using OrchardCore.Deployment;

namespace OrchardCore.Email.ViewModels
{
    public class SmtpSettingsDeploymentStepViewModel
    {
        public DeploymentSecretHandler Password { get; set; }
    }
}
