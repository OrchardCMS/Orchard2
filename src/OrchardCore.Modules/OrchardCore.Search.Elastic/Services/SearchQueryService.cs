using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchardCore.Search.Elastic
{
    public class SearchQueryService : ISearchQueryService
    {
        private readonly ElasticIndexManager _elasticIndexManager;

        private static HashSet<string> IdSet = new HashSet<string>(new string[] { "ContentItemId" });

        public SearchQueryService(ElasticIndexManager elasticIndexManager)
        {
            _elasticIndexManager = elasticIndexManager;
        }

        public async Task<IList<string>> ExecuteQueryAsync(string query, string indexName, int start, int end)
        {
            var contentItemIds = new List<string>();

            await _elasticIndexManager.SearchAsync(indexName, query);

            //Here return the contentItemIds
            return contentItemIds;
        }
    }
}
