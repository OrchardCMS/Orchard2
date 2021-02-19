using System.Threading.Tasks;
using OrchardCore.Documents;
using OrchardCore.Secrets.Models;

namespace OrchardCore.Secrets.Services
{
    public class SecretsDocumentManager
    {
        private readonly IDocumentManager<SecretsDocument> _documentManager;

        public SecretsDocumentManager(IDocumentManager<SecretsDocument> documentManager) => _documentManager = documentManager;


        /// <summary>
        /// Returns the document from the database to be updated.
        /// </summary>
        public Task<SecretsDocument> LoadSecretsDocumentAsync() => _documentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Returns the document from the cache or creates a new one. The result should not be updated.
        /// </summary>
        public Task<SecretsDocument> GetSecretsDocumentAsync() => _documentManager.GetOrCreateImmutableAsync();

        public async Task RemoveSecretAsync(string name)
        {
            var document = await LoadSecretsDocumentAsync();
            document.Secrets.Remove(name);
            await _documentManager.UpdateAsync(document);
        }

        public async Task UpdateSecretAsync(string name, DocumentSecret secret)
        {
            var document = await LoadSecretsDocumentAsync();
            document.Secrets[name] = secret;
            await _documentManager.UpdateAsync(document);
        }
    }
}
