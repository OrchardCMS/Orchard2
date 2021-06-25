using System.Collections.Generic;
using System.Linq;
using OrchardCore.ContentTypes.Services;
using OrchardCore.Localization;

namespace OrchardCore.DataLocalization.Services
{
    public class ContentTypeDataLocalizationProvider : ILocalizationDataProvider
    {
        private readonly IContentDefinitionService _contentDefinitionService;

        private static readonly string ContentTypesContext = "Content Types";

        public ContentTypeDataLocalizationProvider(IContentDefinitionService contentDefinitionService)
        {
            _contentDefinitionService = contentDefinitionService;
        }
        
        public IEnumerable<DataLocalizedString> GetDescriptors()
            => _contentDefinitionService.GetTypes().Select(t => new DataLocalizedString(ContentTypesContext, t.DisplayName));
    }
}
