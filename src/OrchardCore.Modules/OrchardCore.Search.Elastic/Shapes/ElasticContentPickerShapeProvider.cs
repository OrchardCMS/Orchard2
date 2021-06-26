using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.Modules;

namespace OrchardCore.Elastic.Search
{
    [Feature("OrchardCore.Search.Elastic.ContentPicker")]
    public class ElasticContentPickerShapeProvider : IShapeAttributeProvider
    {
        private readonly IStringLocalizer S;

        public ElasticContentPickerShapeProvider(IStringLocalizer<ElasticContentPickerShapeProvider> stringLocalizer)
        {
            S = stringLocalizer;
        }

        [Shape]
        public IHtmlContent ContentPickerField_Option__Elastic(dynamic shape)
        {
            var selected = shape.Editor == "Elastic";
            if (selected)
            {
                return new HtmlString($"<option value=\"Elastic\" selected=\"selected\">{S["Elastic"]}</option>");
            }
            return new HtmlString($"<option value=\"Elastic\">{S["Elastic"]}</option>");
        }
    }
}
