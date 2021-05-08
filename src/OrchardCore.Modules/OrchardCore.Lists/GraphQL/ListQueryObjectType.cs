using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Apis.GraphQL;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.GraphQL.Queries.Types;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Lists.Indexes;
using OrchardCore.Lists.Models;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Lists.GraphQL
{
    public class ListQueryObjectType : ObjectGraphType<ListPart>
    {
        public ListQueryObjectType(IStringLocalizer<ListQueryObjectType> S)
        {
            Name = "ListPart";
            Description = S["Represents a collection of content items."];

            Field<ListGraphType<ContentItemInterface>, IEnumerable<ContentItem>>()
                .Name("contentItems")
                .Description("the content items")
                .Argument<IntGraphType, int>("first", "the first n elements (10 by default)", 10)
                .Argument<IntGraphType, int>("skip", "the number of elements to skip", 0)
                .ResolveAsync(async g =>
                {
                    var serviceProvider = g.RequestServices;
                    var session = serviceProvider.GetService<ISession>();
                    var accessor = serviceProvider.GetRequiredService<IDataLoaderContextAccessor>();

                    var dataLoader = accessor.Context.GetOrAddCollectionBatchLoader<string, ContentItem>("ContainedPublishedContentItems", x => LoadPublishedContentItemsForListAsync(x, session));

                    return ((await dataLoader.LoadAsync(g.Source.ContentItem.ContentItemId).GetResultAsync())
                                .Skip(g.GetArgument<int>("skip"))
                                .Take(g.GetArgument<int>("first")));
                });
        }

        private static async Task<ILookup<string, ContentItem>> LoadPublishedContentItemsForListAsync(IEnumerable<string> contentItemIds, ISession session)
        {
            if (contentItemIds is null || !contentItemIds.Any())
            {
                return new Dictionary<string, ContentItem>().ToLookup(k => k.Key, v => v.Value);
            }

            var query = await session.Query<ContentItem>()
                                     .With<ContentItemIndex>(ci => ci.Published)
                                     .With<ContainedPartIndex>(cp => cp.ListContentItemId.IsIn(contentItemIds))
                                     .OrderBy(o => o.Order)
                                     .ListAsync();

            return query.ToLookup(k => k.As<ContainedPart>().ListContentItemId);
        }
    }
}
