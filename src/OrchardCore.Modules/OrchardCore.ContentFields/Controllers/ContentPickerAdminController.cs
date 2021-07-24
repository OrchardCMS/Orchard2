using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentFields.Controllers
{
    [Admin]
    public class ContentPickerAdminController : Controller
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentPickerResultProvider> _resultProviders;

        public ContentPickerAdminController(
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentPickerResultProvider> resultProviders
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            _resultProviders = resultProviders;
        }

        public async Task<IActionResult> SearchContentItems(string part, string field, string query)
        {
            if (string.IsNullOrWhiteSpace(part) || string.IsNullOrWhiteSpace(field))
            {
                return BadRequest("Part and field are required parameters");
            }

            var partFieldDefinition = _contentDefinitionManager.GetPartDefinition(part)?.Fields
                .FirstOrDefault(f => f.Name == field);

            var fieldSettings = partFieldDefinition?.GetSettings<ContentPickerFieldSettings>();
            if (fieldSettings == null)
            {
                return BadRequest("Unable to find field definition");
            }

            var editor = partFieldDefinition.Editor() ?? "Default";
            var resultProvider = _resultProviders.FirstOrDefault(p => p.Name == editor);
            if (resultProvider == null)
            {
                return new ObjectResult(new List<ContentPickerResult>());
            }

            var results = await resultProvider.Search(new ContentPickerSearchContext
            {
                Query = query,
                DisplayAllContentTypes = fieldSettings.DisplayAllContentTypes,
                ContentTypes = fieldSettings.DisplayedContentTypes,
                PartFieldDefinition = partFieldDefinition,
                TitlePattern = fieldSettings.TitlePattern,
                DescriptionPattern = fieldSettings.DescriptionPattern
            });

            return new ObjectResult(results.Select(r => new VueMultiselectItemViewModel() { Id = r.ContentItemId, DisplayText = r.DisplayText, Description = r.Description, HasPublished = r.HasPublished }));
        }
    }
}
