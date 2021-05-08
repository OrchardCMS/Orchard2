using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement.Records;
using YesSql;
using YesSql.Services;

namespace OrchardCore.ContentManagement.GraphQL
{
    public static class DataLoaderExtensions
    {
        public static IDataLoader<string, IEnumerable<ContentItem>> GetOrAddPublishedContentItemByIdDataLoader<T>(this IResolveFieldContext<T> context)
        {
            var accessor = context.RequestServices.GetRequiredService<IDataLoaderContextAccessor>();
            var session = context.RequestServices.GetService<ISession>();

            return accessor.Context.GetOrAddCollectionBatchLoader<string, ContentItem>("GetPublishedContentItemsById", ci => LoadPublishedContentItemsAsync(ci, session));
        }

        public static async Task<ILookup<string, ContentItem>> LoadPublishedContentItemsAsync(IEnumerable<string> contentItemIds, ISession session)
        {
            if (contentItemIds is null || !contentItemIds.Any())
            {
                return default;
            }

            var contentItemsLoaded = await session.Query<ContentItem, ContentItemIndex>(y => y.ContentItemId.IsIn(contentItemIds) && y.Published).ListAsync();
            return contentItemsLoaded.ToLookup(k => k.ContentItemId, v => v);
        }
    }
}
