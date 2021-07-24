using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Templates.ViewModels;

namespace OrchardCore.Templates.Settings
{
    public class TemplateContentTypeDefinitionDriver : ContentTypeDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;

        public TemplateContentTypeDefinitionDriver(IStringLocalizer<TemplateContentTypeDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentTypeDefinition contentTypeDefinition)
        {
            return Initialize<ContentSettingsViewModel>("TemplateSettings", model =>
            {
                var stereotype = contentTypeDefinition.GetSettings<ContentTypeSettings>().Stereotype;
                var displayName = contentTypeDefinition.DisplayName;

                if (string.IsNullOrWhiteSpace(stereotype))
                {
                    stereotype = "Content";
                }

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{stereotype}__{contentTypeDefinition.Name}",
                        Description = S["{0} content item in detail views", displayName],
                        AdminTemplate = false
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{stereotype}_Summary__{contentTypeDefinition.Name}",
                        Description = S["{0} content item in summary views", displayName],
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
                        Key = $"{stereotype}__DetailAdmin__{contentTypeDefinition.Name}",
                        Description = S["{0} content item in admin detail views", displayName],
                        AdminTemplate = true
                    });

                model.ContentSettingsEntries.Add(
                    new ContentSettingsEntry
                    {
                        Key = $"{stereotype}_SummaryAdmin__{contentTypeDefinition.Name}",
                        Description = S["{0} content item in admin summary views", displayName],
                        AdminTemplate = true
                    });
            }).Location("Shortcuts");
        }
    }
}
