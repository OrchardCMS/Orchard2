using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Placements.ViewModels;

namespace OrchardCore.Placements.Settings
{
    public class PlacementContentTypePartDefinitionDriver : ContentTypePartDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;

        public PlacementContentTypePartDefinitionDriver(IStringLocalizer<PlacementContentTypePartDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentTypePartDefinition contentTypePartDefinition)
        {
            return Initialize<PlacementSettingViewModel>("PlacementSettings", model =>
            {
                var contentType = contentTypePartDefinition.ContentTypeDefinition.Name;
                var partName = contentTypePartDefinition.Name;
                var partDisplayName = contentTypePartDefinition.DisplayName();
                var displayName = contentTypePartDefinition.ContentTypeDefinition.DisplayName;

                var partTypeName = contentTypePartDefinition.PartDefinition.Name;

                var shapeType = partTypeName;
                var differentiator = partName;

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentType = contentType,
                        Description = S["{0} part in a {1} type", partDisplayName, displayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentType = contentType,
                        DisplayType = "Detail",
                        Description = S["{0} part in a {1} type in detail views", partDisplayName, displayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentType = contentType,
                        DisplayType = "Summary",
                        Description = S["{0} part in a {1} type in summary views", partDisplayName, displayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentType = contentType,
                        DisplayType = "DetailAdmin",
                        Description = S["{0} part in a {1} type in admin detail views", partDisplayName, displayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentType = contentType,
                        DisplayType = "SummaryAdmin",
                        Description = S["{0} part in a {1} type in admin summary views", partDisplayName, displayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = $"{partName}_Edit",
                        Differentiator = differentiator,
                        ContentType = contentType,
                        DisplayType = "Edit",
                        Description = S["{0} part in a {1} type in admin editor", partDisplayName, displayName]
                    });

            }).Location("Shortcuts");
        }
    }
}
