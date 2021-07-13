using System.IO;
using Microsoft.Extensions.FileProviders;

namespace OrchardCore.Localization.PortableObject
{
    /// <summary>
    /// Represents a provider that provides a translations for .po files.
    /// </summary>
    public class PoFilesTranslationsProvider : ITranslationProvider
    {
        private readonly ILocalizationFileLocationProvider _poFilesLocationProvider;
        private readonly PortableObjectParser _parser;

        /// <summary>
        /// Creates a new instance of <see cref="PoFilesTranslationsProvider"/>.
        /// </summary>
        /// <param name="poFileLocationProvider">The <see cref="ILocalizationFileLocationProvider"/>.</param>
        public PoFilesTranslationsProvider(ILocalizationFileLocationProvider poFileLocationProvider)
        {
            _poFilesLocationProvider = poFileLocationProvider;
            _parser = new PortableObjectParser();
        }

        /// <inheritdocs />
        public void LoadTranslations(string cultureName, CultureDictionary dictionary)
        {
            foreach (var fileInfo in _poFilesLocationProvider.GetLocations(cultureName))
            {
                LoadFileToDictionary(fileInfo, dictionary);
            }
        }

        private void LoadFileToDictionary(IFileInfo fileInfo, CultureDictionary dictionary)
        {
            if (fileInfo.Exists && !fileInfo.IsDirectory)
            {
                using (var stream = fileInfo.CreateReadStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        dictionary.MergeTranslations(_parser.Parse(reader));
                    }
                }
            }
        }
    }
}
