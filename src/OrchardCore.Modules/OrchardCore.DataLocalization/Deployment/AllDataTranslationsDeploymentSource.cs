using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.Deployment;
using OrchardCore.DataLocalization.Services;

namespace OrchardCore.DataLocalization.Deployment
{
    public class AllDataTranslationsDeploymentSource : IDeploymentSource
    {
        private readonly TranslationsManager _translationsManager;

        public AllDataTranslationsDeploymentSource(TranslationsManager translationsManager)
        {
            _translationsManager = translationsManager ?? throw new ArgumentNullException(nameof(translationsManager));
        }

        public async Task ProcessDeploymentStepAsync(DeploymentStep step, DeploymentPlanResult result)
        {
            var allDataTranslationsState = step as AllDataTranslationsDeploymentStep;

            if (allDataTranslationsState == null)
            {
                return;
            }

            var translationObjects = new JObject();
            var translationsDocument = await _translationsManager.GetTranslationsDocumentAsync();

            foreach (var translation in translationsDocument.Translations)
            {
                translationObjects[translation.Key] = JObject.FromObject(translation.Value);
            }

            result.Steps.Add(new JObject(
                new JProperty("name", "DynamicDataTranslations"),
                new JProperty("DynamicDataTranslations", translationObjects)
            ));

            await Task.CompletedTask;
        }
    }
}
