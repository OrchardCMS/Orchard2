﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Environment.Extensions.Models;

namespace Orchard.DisplayManagement.Descriptors
{
    public class ShapeTableBuilder
    {
        private readonly IList<ShapeAlterationBuilder> _alterationBuilders = new List<ShapeAlterationBuilder>();
        private readonly Feature _feature;

        public ShapeTableBuilder(Feature feature, IList<FeatureDescriptor> excludedFeatures = null)
        {
            _feature = feature;
            ExcludedFeatures = excludedFeatures ?? new List<FeatureDescriptor>();
        }

        public IList<FeatureDescriptor> ExcludedFeatures { get; }

        public ShapeAlterationBuilder Describe(string shapeType)
        {
            var alterationBuilder = new ShapeAlterationBuilder(_feature, shapeType);
            _alterationBuilders.Add(alterationBuilder);
            return alterationBuilder;
        }

        public IEnumerable<ShapeAlteration> BuildAlterations()
        {
            return _alterationBuilders.Select(b => b.Build());
        }
    }
}