using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.DisplayManagement.Zones;

namespace OrchardCore.DisplayManagement
{
    public abstract class BaseDisplayManager
    {
        private readonly IShapeFactory _shapeFactory;
        private readonly IEnumerable<IShapePlacementProvider> _placementProviders;

        public BaseDisplayManager(
            IShapeFactory shapeFactory,
            IEnumerable<IShapePlacementProvider> placementProviders
            )
        {
            _shapeFactory = shapeFactory;
            _placementProviders = placementProviders;
        }

        protected async Task BindPlacementAsync(IBuildShapeContext context)
        {
            var resolvers = new List<IPlacementInfoResolver>();

            foreach (var provider in _placementProviders)
            {
                var resolver = await provider.BuildPlacementInfoResolverAsync(context);

                if (resolver != null)
                {
                    resolvers.Add(resolver);
                }
            }

            context.FindPlacement = (shapeType, differentiator, displayType, displayContext) => FindPlacementImpl(resolvers, shapeType, differentiator, displayType, context);
        }

        private static PlacementInfo FindPlacementImpl(IList<IPlacementInfoResolver> placementResolvers, string shapeType, string differentiator, string displayType, IBuildShapeContext context)
        {
            var delimiterIndex = shapeType.IndexOf("__", StringComparison.Ordinal);

            if (delimiterIndex > 0)
            {
                shapeType = shapeType.Substring(0, delimiterIndex);
            }

            var placementContext = new ShapePlacementContext(
                shapeType,
                displayType,
                differentiator,
                context.Shape
            );

            return placementResolvers.Aggregate<IPlacementInfoResolver, PlacementInfo>(null, (prev, resolver) =>
                CombinePlacements(prev, resolver.ResolvePlacement(placementContext))
            );
        }

        private static PlacementInfo CombinePlacements(PlacementInfo first, PlacementInfo second)
        {
            if (first != null && second != null)
            {
                CombineAlternates(first.Alternates, second.Alternates);
                CombineAlternates(first.Wrappers, second.Wrappers);
                if (!String.IsNullOrEmpty(second.ShapeType))
                {
                    first.ShapeType = second.ShapeType;
                }
                if (!String.IsNullOrEmpty(second.Location))
                {
                    first.Location = second.Location;
                }
                if (!String.IsNullOrEmpty(second.DefaultPosition))
                {
                    first.DefaultPosition = second.DefaultPosition;
                }
                first.Source = second.Source + "," + second.Source;
            }
            else if (second != null)
            {
                return second;
            }
            return first;
        }

        private static AlternatesCollection CombineAlternates(AlternatesCollection first, AlternatesCollection second)
        {
            if (first != null && second != null)
            {
                first.AddRange(second);
            }
            else if (second != null)
            {
                return second;
            }
            return first;
        }

        protected ValueTask<IShape> CreateContentShapeAsync(string actualShapeType)
        {
            return _shapeFactory.CreateAsync(actualShapeType, () =>
                new ValueTask<IShape>(new ZoneHolding(() => _shapeFactory.CreateAsync("ContentZone"))));
        }
    }
}
