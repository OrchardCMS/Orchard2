using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Placements.ViewModels
{
    public class PlacementSettingViewModel
    {
        public List<PlacementSettingEntry> PlacementSettingEntries { get; set; } = new List<PlacementSettingEntry>();
    }

    public class PlacementSettingEntry
    {
        public string ShapeType { get; set; }
        public string DisplayType { get; set; }
        public string ContentType { get; set; }
        public string ContentPart { get; set; }
        public string Differentiator { get; set; }
        public LocalizedString Description { get; set; }
    }
}
