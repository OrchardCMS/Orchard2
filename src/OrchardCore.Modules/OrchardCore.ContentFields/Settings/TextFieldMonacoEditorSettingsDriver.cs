using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.ContentFields.Settings
{
    public class TextFieldMonacoEditorSettingsDriver : ContentPartFieldDefinitionDisplayDriver<TextField>
    {
        private readonly IStringLocalizer S;

        public TextFieldMonacoEditorSettingsDriver(IStringLocalizer<TextFieldMonacoEditorSettingsDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartFieldDefinition partFieldDefinition)
        {
            return Initialize<MonacoSettingsViewModel>("TextFieldMonacoEditorSettings_Edit", model =>
            {
                var settings = partFieldDefinition.GetSettings<TextFieldMonacoEditorSettings>();
                if (string.IsNullOrWhiteSpace(settings.Options))
                {
                    settings.Options = JsonConvert.SerializeObject(new { language = "html" }, Formatting.Indented);
                }
                model.Options = settings.Options;
            })
            .Location("Editor");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPartFieldDefinition partFieldDefinition, UpdatePartFieldEditorContext context)
        {
            if (partFieldDefinition.Editor() == "Monaco")
            {
                var model = new MonacoSettingsViewModel();
                var settings = new TextFieldMonacoEditorSettings();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                if (!model.Options.IsJson())
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(MonacoSettingsViewModel.Options), S["The options are written in an incorrect format."]);
                }
                else
                {
                    settings.Options = model.Options;
                    context.Builder.WithSettings(settings);
                }
            }

            return Edit(partFieldDefinition, context.Updater);
        }
    }
}
