using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Metadata;
using System.Linq;
using OrchardCore.Taxonomies.Fields;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Taxonomies.Services;
using OrchardCore.ContentManagement;
using OrchardCore.Taxonomies.Models;

namespace OrchardCore.Taxonomies.Handlers
{
    public class TaxonomyfieldContentHandler : ContentHandlerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private IContentManager _contentManager;
        private IContentDefinitionManager _contentDefinitionManager;
        private ITaxonomyService _taxonomyService;

        public TaxonomyfieldContentHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task CloningAsync(CloneContentContext context)
        {
            await EnsureFirstPosition(context.CloneContentItem);
        }
        public override async Task ImportingAsync(ImportContentContext context)
        {
            await EnsureFirstPosition(context.ContentItem);
        }

        // Make sure the item gets the first position on every term he is categorized with, if ordering is enabled for the term's taxonomy
        private async Task EnsureFirstPosition(ContentItem contentItem)
        {
            _contentManager ??= _serviceProvider.GetRequiredService<IContentManager>();
            _contentDefinitionManager ??= _serviceProvider.GetRequiredService<IContentDefinitionManager>();
            _taxonomyService ??= _taxonomyService ?? _serviceProvider.GetRequiredService<ITaxonomyService>();

            var fieldDefinitions = _contentDefinitionManager
                    .GetTypeDefinition(contentItem.ContentType)
                    .Parts.SelectMany(x => x.PartDefinition.Fields.Where(f => f.FieldDefinition.Name == nameof(TaxonomyField)))
                    .ToArray();

            foreach (var fieldDefinition in fieldDefinitions)
            {
                var jPart = (JObject)contentItem.Content[fieldDefinition.PartDefinition.Name];
                if (jPart == null)
                {
                    continue;
                }

                var jField = (JObject)jPart[fieldDefinition.Name];
                if (jField == null)
                {
                    continue;
                }

                var field = jField.ToObject<TaxonomyField>();

                var taxonomy = await _contentManager.GetAsync(field.TaxonomyContentItemId, VersionOptions.Latest);
                if (taxonomy != null && taxonomy.As<TaxonomyPart>().EnableOrdering)
                {
                    // Make sure this get's the first position in the list
                    field.TermContentItemOrder.Clear();
                    await _taxonomyService.SyncTaxonomyFieldProperties(field);

                    jField = JObject.FromObject(field);
                    jPart[fieldDefinition.Name] = jField;
                    contentItem.Content[fieldDefinition.PartDefinition.Name] = jPart;
                }
            }
        }
    }
}
