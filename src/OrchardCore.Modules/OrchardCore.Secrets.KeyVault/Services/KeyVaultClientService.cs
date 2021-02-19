using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using OrchardCore.Secrets.KeyVault.Models;

namespace OrchardCore.Secrets.KeyVault.Services
{
    public class KeyVaultClientService
    {
        private readonly SecretClient _secretClient;
        private readonly string _prefix;

        public KeyVaultClientService(IOptions<SecretsKeyVaultOptions> options)
        {

            var keyVaultEndpointUri = new Uri("https://" + options.Value.KeyVaultName + ".vault.azure.net");

            _secretClient = new SecretClient(keyVaultEndpointUri, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeVisualStudioCodeCredential = true }));
            _prefix = options.Value.Prefix;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            if (!String.IsNullOrEmpty(_prefix))
            {
                secretName = _prefix + secretName;
            }

            var secret = await _secretClient.GetSecretAsync(secretName);

            return secret.Value.Value;

        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            if (!String.IsNullOrEmpty(_prefix))
            {
                secretName = _prefix + secretName;
            }

            await _secretClient.SetSecretAsync(secretName, secretValue);
        }

        public async Task RemoveSecretAsync(string secretName)
        {
            var operation = await _secretClient.StartDeleteSecretAsync(secretName);
            // TODO test this. i think we delete secrets on set, so we would need to wait for delete to complete, before updating it again.
            // perhaps not. we can check this.

            // You only need to wait for completion if you want to purge or recover the secret.
            await operation.WaitForCompletionAsync();
        }
    }
}
