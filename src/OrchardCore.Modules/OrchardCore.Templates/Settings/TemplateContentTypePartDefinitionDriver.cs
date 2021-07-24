using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Templates.ViewModels;

namespace OrchardCore.Templates.Settings
{
    public class TemplateContentTypePartDefinitionDriver : ContentTypePartDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;

        public TemplateContentTypePartDefinitionDriver(IStringLocalizer<TemplateContentTypePartDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentTypePartDefinition contentTypePartDefinition)
        {
            return Initialize<ContentSettingsViewModel>("TemplateSettings", model =>
            {
                var contentType = contentTypePartDefinition.ContentTypeDefinition.Name;
                var partName = contentTypePartDefinition.Name;
                var partDisplayName = contentTypePartDefinition.DisplayName();
                var displayName = contentTypePartDefinition.ContentTypeDefinition.DisplayName;

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentType}__{partName}",
                        Description = S["{0} part in a {1} type in detail views", partDisplayName, displayName],
                        AdminTemplate = false

                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentType}_Summary__{partName}",
                        Description = S["{0} part in a {1} type in summary views", partDisplayName, displayName],
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
                        Key = $"{contentType}_DetailAdmin__{partName}",
                        Description = S["{0} part in a {1} type in admin detail views", partDisplayName, displayName],
                        AdminTemplate = true
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{contentType}_SummaryAdmin__{partName}",
                        Description = S["{0} part in a {1} type in admin summary views", partDisplayName, displayName],
                        AdminTemplate = true
                    });
            }).Location("Shortcuts");
        }
    }
}
