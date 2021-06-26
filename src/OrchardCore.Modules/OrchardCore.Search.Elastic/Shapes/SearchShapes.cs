using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Shapes;

namespace OrchardCore.Search.Elastic
{
    public class SearchShapesTableProvider : IShapeTableProvider
    {
        public void Discover(ShapeTableBuilder builder)
        {
            builder.Describe("Search__Form")
                .OnDisplaying(context =>
                {
                    dynamic searchForm = context.Shape;
                });

            builder.Describe("Search__Results")
                .OnDisplaying(context =>
                {
                    dynamic searchResults = context.Shape;
                });
        }
    }

    public class SearchShapes : IShapeAttributeProvider
    {
        private readonly IStringLocalizer S;

        public SearchShapes(IStringLocalizer<SearchShapes> localizer)
        {
            S = localizer;
        }

        [Shape]
        public Task<IHtmlContent> SearchForm(Shape Shape, dynamic DisplayAsync, string Terms)
        {
            Shape.Metadata.Type = "Search__Form";
            return DisplayAsync(Shape);
        }

        [Shape]
        public Task<IHtmlContent> SearchResults(Shape Shape, dynamic DisplayAsync, IEnumerable<ContentItem> ContentItems)
        {
            Shape.Metadata.Type = "Search__Results";
            return DisplayAsync(Shape);
        }
    }
}
