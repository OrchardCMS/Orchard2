using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.DisplayManagement.TagHelpers
{
    public abstract class BaseShapeTagHelper : TagHelper
    {
        private static readonly HashSet<string> InternalProperties = new HashSet<string>
        {
            "id", "type", "cache-id", "cache-context", "cache-dependency", "cache-tag", "cache-fixed-duration", "cache-sliding-duration"
        };

        protected const string PropertyPrefix = "prop-";
        protected IDictionary<string, string> _properties;

        private static readonly char[] Separators = { ',', ' ' };

        protected IShapeFactory _shapeFactory;
        protected IDisplayHelper _displayHelper;

        public string Type { get; set; }
        public string Cache { get; set; }
        public TimeSpan? FixedDuration { get; set; }
        public TimeSpan? SlidingDuration { get; set; }
        public string Context { get; set; }
        public string Tag { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected BaseShapeTagHelper(IShapeFactory shapeFactory, IDisplayHelper displayHelper)
        {
            _shapeFactory = shapeFactory;
            _displayHelper = displayHelper;
        }

        /// <summary>
        /// Additional properties for the shape.
        /// </summary>
        [HtmlAttributeName(DictionaryAttributePrefix = PropertyPrefix)]
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public override async Task ProcessAsync(TagHelperContext tagHelperContext, TagHelperOutput output)
        {
            var properties = new Dictionary<string, object>();

            // These prefixed properties are bound with their original type and not converted as IHtmlContent
            foreach(var property in Properties)
            {
                var normalizedName = property.Key.ToPascalCaseDash();
                properties.Add(normalizedName, property.Value);
            }

            // Extract all other attributes from the tag helper, which are passed as IHtmlContent
            foreach (var pair in output.Attributes)
            {
                // Check it's not a reserved property name
                if (!InternalProperties.Contains(pair.Name))
                {
                    var normalizedName = pair.Name.ToPascalCaseDash();

                    if (!properties.ContainsKey(normalizedName))
                    {
                        properties.Add(normalizedName, pair.Value.ToString());
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(Type))
            {
                Type = output.TagName;
            }

            if (string.IsNullOrWhiteSpace(Cache) && output.Attributes.ContainsName("cache-id"))
            {
                Cache = Convert.ToString(output.Attributes["cache-id"].Value);
            }

            if (string.IsNullOrWhiteSpace(Context) && output.Attributes.ContainsName("cache-context"))
            {
                Context = Convert.ToString(output.Attributes["cache-context"].Value);
            }

            if (string.IsNullOrWhiteSpace(Tag) && output.Attributes.ContainsName("cache-tag"))
            {
                Tag = Convert.ToString(output.Attributes["cache-tag"].Value);
            }

            if (!FixedDuration.HasValue && output.Attributes.ContainsName("cache-fixed-duration"))
            {
                TimeSpan timespan;
                if (TimeSpan.TryParse(Convert.ToString(output.Attributes["cache-fixed-duration"].Value), out timespan))
                {
                    FixedDuration = timespan;
                }
            }

            if (!SlidingDuration.HasValue && output.Attributes.ContainsName("cache-sliding-duration"))
            {
                TimeSpan timespan;
                if (TimeSpan.TryParse(Convert.ToString(output.Attributes["cache-sliding-duration"].Value), out timespan))
                {
                    SlidingDuration = timespan;
                }
            }

            var shape = await _shapeFactory.CreateAsync(Type, Arguments.From(properties));

            if (output.Attributes.ContainsName("id"))
            {
                shape.Id = Convert.ToString(output.Attributes["id"].Value);
            }

            if (output.Attributes.ContainsName("alternate"))
            {
                shape.Metadata.Alternates.Add(Convert.ToString(output.Attributes["alternate"].Value));
            }

            if (output.Attributes.ContainsName("wrapper"))
            {
                shape.Metadata.Wrappers.Add(Convert.ToString(output.Attributes["wrapper"].Value));
            }

            tagHelperContext.Items.Add(typeof(IShape), shape);

            if (!string.IsNullOrWhiteSpace(Cache))
            {
                var metadata = shape.Metadata;

                metadata.Cache(Cache);

                if (FixedDuration.HasValue)
                {
                    metadata.Cache().WithExpiryAfter(FixedDuration.Value);
                }

                if (SlidingDuration.HasValue)
                {
                    metadata.Cache().WithExpirySliding(SlidingDuration.Value);
                }

                if (!string.IsNullOrWhiteSpace(Context))
                {
                    var contexts = Context.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                    metadata.Cache().AddContext(contexts);
                }

                if (!string.IsNullOrWhiteSpace(Tag))
                {
                    var tags = Tag.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                    metadata.Cache().AddTag(tags);
                }
            }

            await output.GetChildContentAsync();

            output.Content.SetHtmlContent(await _displayHelper.ShapeExecuteAsync(shape));

            // We don't want any encapsulating tag around the shape
            output.TagName = null;
        }
    }
}
