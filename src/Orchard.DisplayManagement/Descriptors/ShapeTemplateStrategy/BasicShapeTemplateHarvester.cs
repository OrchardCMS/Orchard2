﻿using System.Collections.Generic;

namespace Orchard.DisplayManagement.Descriptors.ShapeTemplateStrategy
{
    public class BasicShapeTemplateHarvester : IShapeTemplateHarvester
    {
        public const string BasePath = BasePathWithoutTrailingSlash + "/";
        public const string BasePathWithoutTrailingSlash = "Views/Shared/Shapes";

        public IEnumerable<string> SubPaths()
        {
            return new[] { BasePathWithoutTrailingSlash,
                BasePath + "Items",
                BasePath + "Parts",
                BasePath + "Fields",
                BasePath + "Elements"
            };
        }

        public IEnumerable<HarvestShapeHit> HarvestShape(HarvestShapeInfo info)
        {
            var lastDash = info.FileName.LastIndexOf('-');
            var lastDot = info.FileName.LastIndexOf('.');
            if (lastDot <= 0 || lastDot < lastDash)
            {
                yield return new HarvestShapeHit
                {
                    ShapeType = Adjust(info.SubPath, info.FileName, null)
                };
            }
            else
            {
                var displayType = info.FileName.Substring(lastDot + 1);
                yield return new HarvestShapeHit
                {
                    ShapeType = Adjust(info.SubPath, info.FileName.Substring(0, lastDot), displayType),
                    DisplayType = displayType
                };
            }
        }

        static string Adjust(string subPath, string fileName, string displayType)
        {
            var leader = "";
            if (subPath.StartsWith(BasePath) && subPath != (BasePath + "Items"))
            {
                leader = subPath.Substring(BasePath.Length) + "_";
            }

            // canonical shape type names must not have - or . to be compatible
            // with display and shape api calls)))
            var shapeType = leader + fileName.Replace("--", "__").Replace("-", "__").Replace('.', '_');

            if (string.IsNullOrEmpty(displayType))
            {
                return shapeType.ToLowerInvariant();
            }
            var firstBreakingSeparator = shapeType.IndexOf("__");
            if (firstBreakingSeparator <= 0)
            {
                return (shapeType + "_" + displayType).ToLowerInvariant();
            }

            return (shapeType.Substring(0, firstBreakingSeparator) + "_" + displayType + shapeType.Substring(firstBreakingSeparator)).ToLowerInvariant();
        }
    }
}