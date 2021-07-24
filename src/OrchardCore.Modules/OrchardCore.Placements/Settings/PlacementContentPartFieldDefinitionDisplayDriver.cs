using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Placements.ViewModels;

namespace OrchardCore.Placements.Settings
{
    public class PlacementContentPartFieldDefinitionDisplayDriver : ContentPartFieldDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;

        public PlacementContentPartFieldDefinitionDisplayDriver(IStringLocalizer<PlacementContentTypePartDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartFieldDefinition contentPartFieldDefinition)
        {
            return Initialize<PlacementSettingViewModel>("PlacementSettings", model =>
            {
                var shapeType = contentPartFieldDefinition.FieldDefinition.Name;
                var partTypeName = contentPartFieldDefinition.PartDefinition.Name;
                var partDisplayName = contentPartFieldDefinition.PartDefinition.DisplayName();
                var differentiator = $"{contentPartFieldDefinition.PartDefinition.Name}-{contentPartFieldDefinition.Name}";
                var displayName = contentPartFieldDefinition.DisplayName();

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part", displayName, partDisplayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        DisplayType = "Detail",
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part in detail views", displayName, partDisplayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        DisplayType = "Summary",
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part in summary views", displayName, partDisplayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        DisplayType = "DetailAdmin",
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part in admin detail views", displayName, partDisplayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Differentiator = differentiator,
                        DisplayType = "SummaryAdmin",
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part in admin summary views", displayName, partDisplayName]
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = $"{shapeType}_Edit",
                        Differentiator = differentiator,
                        DisplayType = "Edit",
                        ContentPart = partTypeName,
                        Description = S["{0} field in a {1} part in admin editor", displayName, partDisplayName]
                    });

            }).Location("Shortcuts");
        }
    }
}
