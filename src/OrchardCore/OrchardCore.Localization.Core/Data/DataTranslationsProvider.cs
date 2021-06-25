using System.Linq;

namespace OrchardCore.Localization.Data
{
    public class DataTranslationsProvider : ITranslationProvider
    {
        public void LoadTranslations(string cultureName, CultureDictionary dictionary)
        {
            // TODO: Load the translation from the database
            var records = Enumerable.Empty<CultureDictionaryRecord>();
            dictionary.MergeTranslations(records);
        }
    }
}
