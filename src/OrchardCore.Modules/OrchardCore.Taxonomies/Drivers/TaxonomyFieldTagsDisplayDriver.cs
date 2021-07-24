using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Services;
using OrchardCore.Taxonomies.Settings;
using OrchardCore.Taxonomies.ViewModels;

namespace OrchardCore.Taxonomies.Drivers
{
    public class TaxonomyFieldTagsDisplayDriver : ContentFieldDisplayDriver<TaxonomyField>
    {      
        private readonly ITaxonomyService _taxonomyService;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly IContentManager _contentManager;
        private readonly IStringLocalizer S;

        public TaxonomyFieldTagsDisplayDriver(
            IContentManager contentManager,
            IStringLocalizer<TaxonomyFieldTagsDisplayDriver> s,
            ITaxonomyService taxonomyService)
        {
            _contentManager = contentManager;
            _taxonomyService = taxonomyService;
            S = s;
        }

        public override IDisplayResult Display(TaxonomyField field, BuildFieldDisplayContext context)
        {
            return Initialize<DisplayTaxonomyFieldTagsViewModel>(GetDisplayShapeType(context), model =>
            {
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            })
            .Location("Detail", "Content")
            .Location("Summary", "Content");
        }

        public override IDisplayResult Edit(TaxonomyField field, BuildFieldEditorContext context)
        {
            return Initialize<EditTagTaxonomyFieldViewModel>(GetEditorShapeType(context), async model =>
            {
                var settings = context.PartFieldDefinition.GetSettings<TaxonomyFieldSettings>();
                model.Taxonomy = await _contentManager.GetAsync(settings.TaxonomyContentItemId, VersionOptions.Latest);

                if (model.Taxonomy != null)
                {
                    var termEntries = new List<TermEntry>();
                    TaxonomyFieldDriverHelper.PopulateTermEntries(termEntries, field, model.Taxonomy.As<TaxonomyPart>().Terms, 0);
                    var tagTermEntries = termEntries.Select(te => new TagTermEntry
                    { 
                        ContentItemId = te.ContentItemId, 
                        Selected = te.Selected, 
                        DisplayText = te.Term.DisplayText, 
                        IsLeaf = te.IsLeaf
                    });

                    model.TagTermEntries = JsonConvert.SerializeObject(tagTermEntries, SerializerSettings);
                }

                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(TaxonomyField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {
            var model = new EditTagTaxonomyFieldViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix, f => f.TermContentItemIds))
            {
                var settings = context.PartFieldDefinition.GetSettings<TaxonomyFieldSettings>();

                field.TaxonomyContentItemId = settings.TaxonomyContentItemId;
     
                field.TermContentItemIds = model.TermContentItemIds == null
                    ? Array.Empty<string>() : model.TermContentItemIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (settings.Required && field.TermContentItemIds.Length == 0)
                {
                    updater.ModelState.AddModelError(
                        nameof(EditTagTaxonomyFieldViewModel.TermContentItemIds),
                        S["A value is required for '{0}'", context.PartFieldDefinition.DisplayName()]);
                }

                // Update display text for tags.
                var taxonomy = await _contentManager.GetAsync(settings.TaxonomyContentItemId, VersionOptions.Latest);

                if (taxonomy == null)
                {
                    return null;
                }

                var terms = new List<ContentItem>();

                foreach (var termContentItemId in field.TermContentItemIds)
                {
                    var term = TaxonomyOrchardHelperExtensions.FindTerm(taxonomy.Content.TaxonomyPart.Terms as JArray, termContentItemId);
                    terms.Add(term);
                }

                field.SetTagNames(terms.Select(t => t.DisplayText).ToArray());

                if (taxonomy.As<TaxonomyPart>().EnableOrdering)
                {
                    await _taxonomyService.SyncTaxonomyFieldProperties(field);
                }
            }

            return Edit(field, context);
        }
    }
}
