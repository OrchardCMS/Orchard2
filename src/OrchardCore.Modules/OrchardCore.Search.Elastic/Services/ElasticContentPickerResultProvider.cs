using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using OrchardCore.Search.Elastic.Settings;

namespace OrchardCore.Search.Elastic
{
    public class ElasticContentPickerResultProvider : IContentPickerResultProvider
    {
        private readonly ElasticIndexManager _elasticIndexProvider;

        public ElasticContentPickerResultProvider(ElasticIndexManager elasticIndexProvider)
        {
            _elasticIndexProvider = elasticIndexProvider;
        }

        public string Name => "ElasticSearch";

        public async Task<IEnumerable<ContentPickerResult>> Search(ContentPickerSearchContext searchContext)
        {
            //Todo: this needs to be implemented in a different way for Elastic Search
            //Must implement tenantscoped
            var indexName = "Search";

            var fieldSettings = searchContext.PartFieldDefinition?.GetSettings<ContentPickerFieldElasticEditorSettings>();
            if (!string.IsNullOrWhiteSpace(fieldSettings?.Index))
            {
                indexName = fieldSettings.Index;
            }

            if (! await _elasticIndexProvider.Exists(indexName))
            {
                return new List<ContentPickerResult>();
            }

            var results = new List<ContentPickerResult>();

            //await _elasticIndexProvider.SearchAsync(indexName, searcher =>
            //{
            //    Query query = null;

            //    if (string.IsNullOrWhiteSpace(searchContext.Query))
            //    {
            //        query = new MatchAllDocsQuery();
            //    }
            //    else
            //    {
            //        query = new WildcardQuery(new Term("Content.ContentItem.DisplayText.Analyzed", searchContext.Query.ToLowerInvariant() + "*"));
            //    }

            //    var filter = new FieldCacheTermsFilter("Content.ContentItem.ContentType", searchContext.ContentTypes.ToArray());

            //    var docs = searcher.Search(query, filter, 50, Sort.RELEVANCE);

            //    foreach (var hit in docs.ScoreDocs)
            //    {
            //        var doc = searcher.Doc(hit.Doc);

            //        results.Add(new ContentPickerResult
            //        {
            //            ContentItemId = doc.GetField("ContentItemId").GetStringValue(),
            //            DisplayText = doc.GetField("Content.ContentItem.DisplayText").GetStringValue(),
            //            HasPublished = doc.GetField("Content.ContentItem.Published").GetStringValue() == "true" ? true : false
            //        });
            //    }

            //    return Task.CompletedTask;
            //});

            return results.OrderBy(x => x.DisplayText);
        }
    }
}
