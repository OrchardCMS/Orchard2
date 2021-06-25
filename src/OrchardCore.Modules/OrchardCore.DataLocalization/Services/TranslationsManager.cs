using System.Threading.Tasks;
using OrchardCore.Documents;
using OrchardCore.DataLocalization.Models;

namespace OrchardCore.DataLocalization.Services
{
    public class TranslationsManager : ITranslationsManager
    {
        private readonly IDocumentManager<TranslationsDocument> _documentManager;

        public TranslationsManager(IDocumentManager<TranslationsDocument> documentManager) => _documentManager = documentManager;

        public Task<TranslationsDocument> LoadTranslationsDocumentAsync() => _documentManager.GetOrCreateMutableAsync();

        public Task<TranslationsDocument> GetTranslationsDocumentAsync() => _documentManager.GetOrCreateImmutableAsync();

        public async Task RemoveTranslationAsync(string name)
        {
            var document = await LoadTranslationsDocumentAsync();
            document.Translations.Remove(name);

            await _documentManager.UpdateAsync(document);
        }

        public async Task UpdateTranslationAsync(string name, Translation translation)
        {
            var document = await LoadTranslationsDocumentAsync();
            document.Translations[name] = translation;

            await _documentManager.UpdateAsync(document);
        }
    }
}
