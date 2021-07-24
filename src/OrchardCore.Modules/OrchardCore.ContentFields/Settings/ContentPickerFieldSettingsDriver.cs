using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Liquid;

namespace OrchardCore.ContentFields.Settings
{
    public class ContentPickerFieldSettingsDriver : ContentPartFieldDefinitionDisplayDriver<ContentPickerField>
    {
        private readonly ILiquidTemplateManager _templateManager;
        private readonly IStringLocalizer S;

        public ContentPickerFieldSettingsDriver(ILiquidTemplateManager templateManager, IStringLocalizer<ContentPickerFieldSettingsDriver> localizer)
        {
            _templateManager = templateManager;
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartFieldDefinition partFieldDefinition)
        {
            return Initialize<ContentPickerFieldSettings>("ContentPickerFieldSettings_Edit", model => partFieldDefinition.PopulateSettings(model))
                .Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPartFieldDefinition partFieldDefinition, UpdatePartFieldEditorContext context)
        {
            var model = new ContentPickerFieldSettings();

            await context.Updater.TryUpdateModelAsync(model, Prefix);

            if (model.DisplayAllContentTypes)
            {
                model.DisplayedContentTypes = Array.Empty<String>();
            }

            bool isValid = true;

            if (!string.IsNullOrEmpty(model.DescriptionPattern) && !_templateManager.Validate(model.DescriptionPattern, out var descriptionError))
            {
                context.Updater.ModelState.AddModelError(nameof(model.DescriptionPattern), S["DescriptionPattern doesn't contain a valid Liquid expression. Details: {0}", string.Join(" ", descriptionError)]);
                isValid = false;
            }

            if (!string.IsNullOrEmpty(model.TitlePattern) && !_templateManager.Validate(model.TitlePattern, out var titleErrors))
            {
                context.Updater.ModelState.AddModelError(nameof(model.TitlePattern), S["TitlePattern doesn't contain a valid Liquid expression. Details: {0}", string.Join(" ", titleErrors)]);
                isValid = false;
            }

            if (isValid)
            { 
                context.Builder.WithSettings(model);
            }

            return Edit(partFieldDefinition);
        }
    }
}
