using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.Modules;

namespace OrchardCore.Lucene.ContentPicker
{
    [Feature("OrchardCore.Lucene.ContentPicker")]
    public class LuceneContentPickerShapesTableProvider : IShapeTableProvider
    {
        public void Discover(ShapeTableBuilder builder)
        {
            builder.Describe("ContentPickerField_Option__Lucene");
        }
    }

    public class LuceneContentPickerShape : IShapeAttributeProvider
    {
        private readonly IStringLocalizer<LuceneContentPickerShape> S;

        public LuceneContentPickerShape(IStringLocalizer<LuceneContentPickerShape> stringLocalizer)
        {
            S = stringLocalizer;
        }

        [Shape]
        public IHtmlContent ContentPickerField_Option__Lucene(dynamic Shape)
        {
            var selected = Shape.Editor == "Lucene";
            if (selected)
            {
                return new HtmlString($"<option value=\"Lucene\" selected=\"selected\">{S["Lucene"]}</option>");
            }
            return new HtmlString($"<option value=\"Lucene\">{S["Lucene"]}</option>");
        }
    }
}
