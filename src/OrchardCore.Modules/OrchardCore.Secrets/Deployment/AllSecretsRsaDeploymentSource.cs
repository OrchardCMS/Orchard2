using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Deployment;

namespace OrchardCore.Secrets.Deployment
{
    public class AllSecretsRsaDeploymentSource : IDeploymentSource
    {
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ISecretCoordinator _secretCoordinator;
        private readonly IEnumerable<ISecretFactory> _factories;

        public AllSecretsRsaDeploymentSource(
            IEncryptionProvider encryptionProvider,
            ISecretCoordinator secretCoordinator,
            IEnumerable<ISecretFactory> factories)
        {
            _encryptionProvider = encryptionProvider;
            _secretCoordinator = secretCoordinator;
            _factories = factories;
        }

        public async Task ProcessDeploymentStepAsync(DeploymentStep deploymentStep, DeploymentPlanResult result)
        {
            if (!(deploymentStep is AllSecretsRsaDeploymentStep allSecretsDeploymentStep))
            {
                return;
            }

            var secretBindings = await _secretCoordinator.GetSecretBindingsAsync();

            if (!secretBindings.Any())
            {
                return;
            }

            if (String.IsNullOrEmpty(result.EncryptionSecretName))
            {
                throw new InvalidOperationException("You must set an rsa secret for the deployment target before exporting secrets");
            }

            var encryptor = await _encryptionProvider.CreateAsync(result.EncryptionSecretName, result.SigningSecretName);

            var secrets = new Dictionary<string, JObject>();
            foreach (var secretBinding in secretBindings)
            {
                var secretDescriptor = _secretCoordinator.FirstOrDefault(x => String.Equals(x.Name, secretBinding.Value.Store, StringComparison.OrdinalIgnoreCase));
                // When descriptor is readonly we ship a binding without the secret value.
                var jObject = new JObject(new JProperty("SecretBinding", JObject.FromObject(secretBinding.Value)));

                if (!secretDescriptor.IsReadOnly)
                {
                    var secret = _factories.FirstOrDefault(x => x.Name == secretBinding.Value.Type)?.Create();
                    secret = await _secretCoordinator.GetSecretAsync(secretBinding.Key, secret.GetType());
                    if (secret != null)
                    {
                        var plaintext = JsonConvert.SerializeObject(secret);

                        var encrypted = encryptor.Encrypt(plaintext);

                        // [js: decrypt('theaesencryptionkey', 'theencryptedvalue')]
                        jObject.Add("Secret", $"[js: decrypt('{encrypted}')]");
                    }
                }
                secrets.Add(secretBinding.Key, jObject);
            }

            result.Steps.Add(new JObject(
                new JProperty("name", "Secrets"),
                new JProperty("Secrets", JObject.FromObject(secrets))
            ));
        }
    }
}
