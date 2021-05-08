using System.Text.Encodings.Web;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Abstractions.Pooling;
using OrchardCore.Apis.GraphQL;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.GraphQL.Options;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.ContentManagement.GraphQL.Queries.Types
{
    public class ContentItemType : ObjectGraphType<ContentItem>
    {
        private readonly GraphQLContentOptions _options;

        public ContentItemType(IOptions<GraphQLContentOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;

            Name = "ContentItemType";

            Field(ci => ci.ContentItemId).Description("Content item id");
            Field(ci => ci.ContentItemVersionId).Description("The content item version id");
            Field(ci => ci.ContentType).Description("Type of content");
            Field(ci => ci.DisplayText, nullable: true).Description("The display text of the content item");
            Field(ci => ci.Published).Description("Is the published version");
            Field(ci => ci.Latest).Description("Is the latest version");
            Field<DateTimeGraphType>("modifiedUtc", resolve: ci => ci.Source.ModifiedUtc, description: "The date and time of modification");
            Field<DateTimeGraphType>("publishedUtc", resolve: ci => ci.Source.PublishedUtc, description: "The date and time of publication");
            Field<DateTimeGraphType>("createdUtc", resolve: ci => ci.Source.CreatedUtc, description: "The date and time of creation");
            Field(ci => ci.Owner).Description("The owner of the content item");
            Field(ci => ci.Author).Description("The author of the content item");

            Field<StringGraphType, string>()
                .Name("render")
                .ResolveLockedAsync(async context =>
                {
                    var serviceProvider = context.RequestServices;

                    // Build shape
                    var displayManager = serviceProvider.GetRequiredService<IContentItemDisplayManager>();
                    var updateModelAccessor = serviceProvider.GetRequiredService<IUpdateModelAccessor>();
                    var model = await displayManager.BuildDisplayAsync(context.Source, updateModelAccessor.ModelUpdater);

                    var displayHelper = serviceProvider.GetRequiredService<IDisplayHelper>();
                    var htmlEncoder = serviceProvider.GetRequiredService<HtmlEncoder>();

                    using var sw = new ZStringWriter();
                    var htmlContent = await displayHelper.ShapeExecuteAsync(model);
                    htmlContent.WriteTo(sw, htmlEncoder);
                    return sw.ToString();
                });

            Interface<ContentItemInterface>();

            IsTypeOf = IsContentType;
        }

        private bool IsContentType(object obj)
        {
            return obj is ContentItem item && item.ContentType == Name;
        }

        public override FieldType AddField(FieldType fieldType)
        {
            if (!_options.ShouldSkip(this.GetType(), fieldType.Name))
            {
                return base.AddField(fieldType);
            }

            return null;
        }
    }
}
