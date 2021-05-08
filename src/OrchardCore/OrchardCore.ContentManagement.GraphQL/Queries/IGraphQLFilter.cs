using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using YesSql;

namespace OrchardCore.ContentManagement.GraphQL.Queries
{
    public interface IGraphQLFilter<TSourceType> where TSourceType : class
    {
        Task<IQuery<TSourceType>> PreQueryAsync(IQuery<TSourceType> query, IResolveFieldContext context);

        Task<IEnumerable<TSourceType>> PostQueryAsync(IEnumerable<TSourceType> contentItems, IResolveFieldContext context);
    }
}
