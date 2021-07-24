using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OrchardCore.DisplayManagement.TagHelpers
{
    [HtmlTargetElement("datetime")]
    public class DateTimeTagHelper : TagHelper
    {
        private const string UtcAttribute = "utc";
        private const string FormatAttribute = "format";

        protected IShapeFactory _shapeFactory;
        protected IDisplayHelper _displayHelper;

        public DateTimeTagHelper(IShapeFactory shapeFactory, IDisplayHelper displayHelper)
        {
            _shapeFactory = shapeFactory;
            _displayHelper = displayHelper;
        }

        [HtmlAttributeName(UtcAttribute)]
        public DateTime? Utc { set; get; }

        [HtmlAttributeName(FormatAttribute)]
        public string Format { set; get; }

        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var shapeType = "DateTime";
            dynamic shape = await _shapeFactory.CreateAsync(shapeType);
            shape.Utc = Utc;
            shape.Format = Format;
            
            output.Content.SetHtmlContent(await _displayHelper.ShapeExecuteAsync(shape));

            // We don't want any encapsulating tag around the shape
            output.TagName = null;
        }
    }
}