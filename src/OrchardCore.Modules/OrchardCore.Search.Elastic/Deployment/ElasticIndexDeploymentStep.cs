using OrchardCore.Deployment;

namespace OrchardCore.Search.Elastic.Deployment
{
    /// <summary>
    /// Adds layers to a <see cref="DeploymentPlanResult"/>.
    /// </summary>
    public class ElasticIndexDeploymentStep : DeploymentStep
    {
        public ElasticIndexDeploymentStep()
        {
            Name = "ElasticIndex";
        }

        public bool IncludeAll { get; set; } = true;

        public string[] IndexNames { get; set; }
    }
}
