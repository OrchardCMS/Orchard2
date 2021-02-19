using System.Threading.Tasks;
using OrchardCore.Documents;
using OrchardCore.Secrets.Models;

namespace OrchardCore.Secrets.Services
{
    public class SecretBindingsManager
    {
        private readonly IDocumentManager<SecretBindingsDocument> _documentManager;
        public SecretBindingsManager(IDocumentManager<SecretBindingsDocument> documentManager) => _documentManager = documentManager;

        /// <summary>
        /// Returns the document from the database to be updated.
        /// </summary>
        public Task<SecretBindingsDocument> LoadSecretBindingsDocumentAsync() => _documentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Returns the document from the cache or creates a new one. The result should not be updated.
        /// </summary>
        public Task<SecretBindingsDocument> GetSecretBindingsDocumentAsync() => _documentManager.GetOrCreateImmutableAsync();

        public async Task RemoveSecretBindingAsync(string name)
        {
            var document = await LoadSecretBindingsDocumentAsync();
            document.SecretBindings.Remove(name);
            await _documentManager.UpdateAsync(document);
        }

        public async Task UpdateSecretBindingAsync(string name, SecretBinding secretBinding)
        {
            var document = await LoadSecretBindingsDocumentAsync();
            document.SecretBindings[name] = secretBinding;
            await _documentManager.UpdateAsync(document);
        }
    }
}
