using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Apis.GraphQL;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Html.Models;
using OrchardCore.Html.Settings;
using OrchardCore.Html.ViewModels;
using OrchardCore.Liquid;
using OrchardCore.Shortcodes.Services;
using Shortcodes;

namespace OrchardCore.Html.GraphQL
{
    public class HtmlBodyQueryObjectType : ObjectGraphType<HtmlBodyPart>
    {
        public HtmlBodyQueryObjectType(IStringLocalizer<HtmlBodyQueryObjectType> S)
        {
            Name = "HtmlBodyPart";
            Description = S["Content stored as HTML."];

            Field<StringGraphType>()
                .Name("html")
                .Description(S["the HTML content"])
                .ResolveLockedAsync(RenderHtml);
        }

        private static async Task<object> RenderHtml(IResolveFieldContext<HtmlBodyPart> ctx)
        {
            var shortcodeService = ctx.RequestServices.GetRequiredService<IShortcodeService>();
            var contentDefinitionManager = ctx.RequestServices.GetRequiredService<IContentDefinitionManager>();

            var contentTypeDefinition = contentDefinitionManager.GetTypeDefinition(ctx.Source.ContentItem.ContentType);
            var contentTypePartDefinition = contentTypeDefinition.Parts.FirstOrDefault(x => string.Equals(x.PartDefinition.Name, "HtmlBodyPart"));
            var settings = contentTypePartDefinition.GetSettings<HtmlBodyPartSettings>();

            var html = ctx.Source.Html;

            if (!settings.SanitizeHtml)
            {
                var model = new HtmlBodyPartViewModel()
                {
                    Html = ctx.Source.Html,
                    HtmlBodyPart = ctx.Source,
                    ContentItem = ctx.Source.ContentItem
                };
                var liquidTemplateManager = ctx.RequestServices.GetRequiredService<ILiquidTemplateManager>();
                var htmlEncoder = ctx.RequestServices.GetService<HtmlEncoder>();

                html = await liquidTemplateManager.RenderStringAsync(html, htmlEncoder, model, new Dictionary<string, FluidValue> { ["ContentItem"] = new ObjectValue(model.ContentItem) });
            }

            return await shortcodeService.ProcessAsync(html,
                new Context
                {
                    ["ContentItem"] = ctx.Source.ContentItem,
                    ["TypePartDefinition"] = contentTypePartDefinition
                });
        }
    }
}
