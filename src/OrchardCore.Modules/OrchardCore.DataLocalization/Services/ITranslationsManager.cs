using System.Threading.Tasks;
using OrchardCore.DataLocalization.Models;

namespace OrchardCore.DataLocalization.Services
{
    public interface ITranslationsManager
    {
        Task<TranslationsDocument> LoadTranslationsDocumentAsync();

        Task<TranslationsDocument> GetTranslationsDocumentAsync();

        Task RemoveTranslationAsync(string name);

        Task UpdateTranslationAsync(string name, Translation translation);
    }
}
