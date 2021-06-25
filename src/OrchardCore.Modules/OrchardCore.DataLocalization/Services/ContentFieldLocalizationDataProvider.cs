using System.Collections.Generic;
using System.Linq;
using OrchardCore.ContentTypes.Services;
using OrchardCore.Localization;

namespace OrchardCore.DataLocalization.Services
{
    public class ContentFieldDataLocalizationProvider : ILocalizationDataProvider
    {
        private readonly IContentDefinitionService _contentDefinitionService;

        private static readonly string ContentFieldsContext = "Content Fields";

        public ContentFieldDataLocalizationProvider(IContentDefinitionService contentDefinitionService)
        {
            _contentDefinitionService = contentDefinitionService;
        }
        
        // TODO: Check if there's a better way to get the fields
        public IEnumerable<DataLocalizedString> GetDescriptors()
            => _contentDefinitionService.GetTypes()
                    .SelectMany(t => t.TypeDefinition.Parts)
                    .SelectMany(p => p.PartDefinition.Fields.Select(f => new DataLocalizedString(ContentFieldsContext, f.Name)));
    }
}
