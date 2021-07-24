using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Templates.ViewModels;

namespace OrchardCore.Templates.Settings
{
    public class TemplateContentPartDefinitionDriver : ContentPartDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;

        public TemplateContentPartDefinitionDriver(IStringLocalizer<TemplateContentPartDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartDefinition contentPartDefinition)
        {
            var displayName = contentPartDefinition.DisplayName();

            return Initialize<ContentSettingsViewModel>("TemplateSettings", model =>
            {
                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = contentPartDefinition.Name,
                        Description = S["{0} part in detail views", displayName],
                        AdminTemplate = false
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentPartDefinition.Name}_Summary",
                        Description = S["{0} part in summary views", displayName],
                        AdminTemplate = false
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Description = S["-"]
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentPartDefinition.Name}_DetailAdmin",
                        Description = S["{0} part in admin detail views", displayName],
                        AdminTemplate = true
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentPartDefinition.Name}_SummaryAdmin",
                        Description = S["{0} part in admin summary views", displayName],
                        AdminTemplate = true
                    });
            }).Location("Shortcuts");
        }
    }
}
