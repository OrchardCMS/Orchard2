using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Placements.ViewModels;

namespace OrchardCore.Placements.Settings
{
    public class PlacementContentPartDefinitionDriver : ContentPartDefinitionDisplayDriver
    {
        private readonly IStringLocalizer S;
        public PlacementContentPartDefinitionDriver(IStringLocalizer<PlacementContentPartDefinitionDriver> localizer)
        {
            S = localizer;
        }

        public override IDisplayResult Edit(ContentPartDefinition contentPartDefinition)
        {
            var displayName = contentPartDefinition.DisplayName();
            var partName = contentPartDefinition.Name;

            var shapeType = partName;

            return Initialize<PlacementSettingViewModel>("PlacementSettings", model =>
            {
                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        Description = S["{0} part", displayName],
                        ContentPart = partName
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        DisplayType = "Detail",
                        Description = S["{0} part in detail views", displayName],
                        ContentPart = partName
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        DisplayType = "Summary",
                        Description = S["{0} part in summary views", displayName],
                        ContentPart = partName
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        DisplayType = "DetailAdmin",
                        Description = S["{0} part in admin detail views", displayName],
                        ContentPart = partName
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = shapeType,
                        DisplayType = "SummaryAdmin",
                        Description = S["{0} part in admin summary views", displayName],
                        ContentPart = partName
                    });

                model.PlacementSettingEntries.Add(
                    new PlacementSettingEntry
                    {
                        ShapeType = $"{shapeType}_Edit",
                        Description = S["{0} part in admin editor", displayName],
                        DisplayType = "Edit",
                        ContentPart = partName
                    });

            }).Location("Shortcuts");
        }
    }
}
