using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.Deployment;

namespace OrchardCore.Search.Elastic.Deployment
{
    public class ElasticSettingsDeploymentSource : IDeploymentSource
    {
        private readonly ElasticIndexingService _elasticIndexingService;

        public ElasticSettingsDeploymentSource(ElasticIndexingService elasticIndexingService)
        {
            _elasticIndexingService = elasticIndexingService;
        }

        public async Task ProcessDeploymentStepAsync(DeploymentStep step, DeploymentPlanResult result)
        {
            var elasticSettingsStep = step as ElasticSettingsDeploymentStep;

            if (elasticSettingsStep == null)
            {
                return;
            }

            var elasticSettings = await _elasticIndexingService.GetElasticSettingsAsync();

            // Adding Elastic settings
            result.Steps.Add(new JObject(
                new JProperty("name", "Settings"),
                new JProperty("ElaticSettings", JObject.FromObject(elasticSettings))
            ));
        }
    }
}
