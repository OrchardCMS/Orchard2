using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using YesSql;

namespace OrchardCore.ContentManagement.GraphQL.Queries
{
    public abstract class GraphQLFilter<TSourceType> : IGraphQLFilter<TSourceType> where TSourceType : class
    {
        public virtual Task<IQuery<TSourceType>> PreQueryAsync(IQuery<TSourceType> query, IResolveFieldContext context)
        {
            return Task.FromResult(query);
        }

        public virtual Task<IEnumerable<TSourceType>> PostQueryAsync(IEnumerable<TSourceType> contentItems, IResolveFieldContext context)
        {
            return Task.FromResult(contentItems);
        }
    }
}
