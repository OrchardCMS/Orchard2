using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Apis.GraphQL;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.GraphQL.Queries.Types;

namespace OrchardCore.ContentLocalization.GraphQL
{
    public class LocalizationQueryObjectType : ObjectGraphType<LocalizationPart>
    {
        public LocalizationQueryObjectType(IStringLocalizer<LocalizationQueryObjectType> S)
        {
            Name = "LocalizationPart";
            Description = S["Localization cultures for your content item."];

            Field(x => x.Culture).Description(S["The culture for your content item."]);
            Field(x => x.LocalizationSet).Description(S["The localization set for your content item."]);

            Field<ListGraphType<ContentItemInterface>, IEnumerable<ContentItem>>()
                .Name("Localizations")
                .Description(S["The localizations of the content item."])
                .Argument<StringGraphType, string>("culture", "the culture of the content item")
                .ResolveLockedAsync(async ctx =>
               {
                   var culture = ctx.GetArgument<string>("culture");
                   var contentLocalizationManager = ctx.RequestServices.GetService<IContentLocalizationManager>();

                   if (culture != null)
                   {
                       var contentItem = await contentLocalizationManager.GetContentItemAsync(ctx.Source.LocalizationSet, culture);

                       return contentItem != null ? new[] { contentItem } : Enumerable.Empty<ContentItem>();
                   }

                   return await contentLocalizationManager.GetItemsForSetAsync(ctx.Source.LocalizationSet);
               });
        }
    }
}
