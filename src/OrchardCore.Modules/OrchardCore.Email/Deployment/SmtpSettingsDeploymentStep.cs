using OrchardCore.Deployment;

namespace OrchardCore.Email.Deployment
{
    /// <summary>
    /// Adds smtp settings to a <see cref="DeploymentPlanResult"/>.
    /// </summary>
    public class SmtpSettingsDeploymentStep : DeploymentStep
    {
        public SmtpSettingsDeploymentStep()
        {
            Name = "SmtpSettings";
        }

        public DeploymentSecretHandler Password { get; set; }
    }
}
