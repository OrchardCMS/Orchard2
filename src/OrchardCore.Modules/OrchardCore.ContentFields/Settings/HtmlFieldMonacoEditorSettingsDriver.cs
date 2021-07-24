using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.ContentFields.Settings
{
    public class HtmlFieldMonacoEditorSettingsDriver : ContentPartFieldDefinitionDisplayDriver<HtmlField>
    {
        private readonly IStringLocalizer S;

        public HtmlFieldMonacoEditorSettingsDriver(IStringLocalizer<HtmlFieldMonacoEditorSettingsDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartFieldDefinition partFieldDefinition)
        {
            return Initialize<MonacoSettingsViewModel>("HtmlFieldMonacoEditorSettings_Edit", model =>
            {
                var settings = partFieldDefinition.GetSettings<HtmlFieldMonacoEditorSettings>();
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
                var settings = new HtmlFieldMonacoEditorSettings();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                if (!model.Options.IsJson())
                {
                    context.Updater.ModelState.AddModelError(Prefix + "." + nameof(MonacoSettingsViewModel.Options), S["The options are written in an incorrect format."]);
                }
                else
                {
                    var jsonSettings = JObject.Parse(model.Options);
                    jsonSettings["language"] = "html";
                    settings.Options = jsonSettings.ToString();
                    context.Builder.WithSettings(settings);
                }
            }

            return Edit(partFieldDefinition, context.Updater);
        }
    }
}
