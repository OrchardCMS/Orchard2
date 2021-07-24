using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using OrchardCore.ContentManagement;
using OrchardCore.Lucene.Settings;

namespace OrchardCore.Lucene.Services
{
    public class LuceneContentPickerResultProvider : IContentPickerResultProvider
    {
        private readonly LuceneIndexManager _luceneIndexProvider;

        public LuceneContentPickerResultProvider(LuceneIndexManager luceneIndexProvider)
        {
            _luceneIndexProvider = luceneIndexProvider;
        }

        public string Name => "Lucene";

        public async Task<IEnumerable<ContentPickerResult>> Search(ContentPickerSearchContext searchContext)
        {
            var indexName = "Search";

            var fieldSettings = searchContext.PartFieldDefinition?.GetSettings<ContentPickerFieldLuceneEditorSettings>();
            if (!string.IsNullOrWhiteSpace(fieldSettings?.Index))
            {
                indexName = fieldSettings.Index;
            }

            if (!_luceneIndexProvider.Exists(indexName))
            {
                return new List<ContentPickerResult>();
            }

            var results = new List<ContentPickerResult>();

            await _luceneIndexProvider.SearchAsync(indexName, searcher =>
            {
                Query query = null;

                if (string.IsNullOrWhiteSpace(searchContext.Query))
                {
                    query = new MatchAllDocsQuery();
                }
                else
                {
                    query = new WildcardQuery(new Term("Content.ContentItem.DisplayText.Analyzed", searchContext.Query.ToLowerInvariant() + "*"));
                }

                var filter = new FieldCacheTermsFilter("Content.ContentItem.ContentType", searchContext.ContentTypes.ToArray());

                var docs = searcher.Search(query, filter, 50, Sort.RELEVANCE);

                foreach (var hit in docs.ScoreDocs)
                {
                    var doc = searcher.Doc(hit.Doc);

                    results.Add(new ContentPickerResult
                    {
                        ContentItemId = doc.GetField("ContentItemId").GetStringValue(),
                        DisplayText = doc.GetField("Content.ContentItem.DisplayText").GetStringValue(),
                        HasPublished = doc.GetField("Content.ContentItem.Published").GetStringValue() == "true" ? true : false,
                    });
                }

                return Task.CompletedTask;
            });

            return results.OrderBy(x => x.DisplayText);
        }

        public Task<string> GetContentPickerItemDescription(ContentItem contentItem, string pattern, string defaultValue)
        {
            return Task.FromResult(defaultValue);
        }
    }
}
