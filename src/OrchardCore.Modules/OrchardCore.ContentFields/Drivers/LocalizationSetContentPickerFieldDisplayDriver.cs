using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Modules;

namespace OrchardCore.ContentFields.Fields
{
    [RequireFeatures("OrchardCore.ContentLocalization")]
    public class LocalizationSetContentPickerFieldDisplayDriver : ContentFieldDisplayDriver<LocalizationSetContentPickerField>
    {
        private readonly IContentManager _contentManager;
        private readonly IContentLocalizationManager _contentLocalizationManager;
        private readonly IStringLocalizer S;

        public LocalizationSetContentPickerFieldDisplayDriver(
            IContentManager contentManager,
            IStringLocalizer<LocalizationSetContentPickerFieldDisplayDriver> localizer,
            IContentLocalizationManager contentLocalizationManager)
        {
            _contentManager = contentManager;
            S = localizer;
            _contentLocalizationManager = contentLocalizationManager;
        }

        public override IDisplayResult Display(LocalizationSetContentPickerField field, BuildFieldDisplayContext context)
        {
            return Initialize<DisplayLocalizationSetContentPickerFieldViewModel>(GetDisplayShapeType(context), model =>
            {
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            })
            .Location("Detail", "Content")
            .Location("Summary", "Content");
        }

        public override IDisplayResult Edit(LocalizationSetContentPickerField field, BuildFieldEditorContext context)
        {
            return Initialize<EditLocalizationSetContentPickerFieldViewModel>(GetEditorShapeType(context), async model =>
            {
                model.LocalizationSets = string.Join(",", field.LocalizationSets);

                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;

                model.SelectedItems = new List<VueMultiselectItemViewModel>();

                foreach (var kvp in await _contentLocalizationManager.GetFirstItemIdForSetsAsync(field.LocalizationSets))
                {
                    var contentItem = await _contentManager.GetAsync(kvp.Value, VersionOptions.Latest);

                    if (contentItem == null)
                    {
                        continue;
                    }

                    model.SelectedItems.Add(new VueMultiselectItemViewModel
                    {
                        Id = kvp.Key, //localization set
                        DisplayText = contentItem.ToString(),
                        HasPublished = await _contentManager.HasPublishedVersionAsync(contentItem)
                    });
                }
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(LocalizationSetContentPickerField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {
            var viewModel = new EditLocalizationSetContentPickerFieldViewModel();

            var modelUpdated = await updater.TryUpdateModelAsync(viewModel, Prefix, f => f.LocalizationSets);

            if (!modelUpdated)
            {
                return Edit(field, context);
            }

            field.LocalizationSets = viewModel.LocalizationSets == null
                ? new string[0] : viewModel.LocalizationSets.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var settings = context.PartFieldDefinition.GetSettings<LocalizationSetContentPickerFieldSettings>();

            if (settings.Required && field.LocalizationSets.Length == 0)
            {
                updater.ModelState.AddModelError(Prefix, S["The {0} field is required.", context.PartFieldDefinition.DisplayName()]);
            }

            if (!settings.Multiple && field.LocalizationSets.Length > 1)
            {
                updater.ModelState.AddModelError(Prefix, S["The {0} field cannot contain multiple items.", context.PartFieldDefinition.DisplayName()]);
            }

            return Edit(field, context);
        }
    }
}
