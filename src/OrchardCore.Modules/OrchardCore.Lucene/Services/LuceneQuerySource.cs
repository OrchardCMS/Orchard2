using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Liquid;
using OrchardCore.Lucene.Model;
using OrchardCore.Lucene.Services;
using OrchardCore.Queries;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Lucene
{
    public class LuceneQuerySource : IQuerySource
    {
        private readonly LuceneIndexManager _luceneIndexProvider;
        private readonly LuceneIndexSettingsService _luceneIndexSettingsService;
        private readonly LuceneAnalyzerManager _luceneAnalyzerManager;
        private readonly ILuceneQueryService _queryService;
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly ISession _session;
        private readonly JavaScriptEncoder _javaScriptEncoder;
        private readonly TemplateOptions _templateOptions;

        public LuceneQuerySource(
            LuceneIndexManager luceneIndexProvider,
            LuceneIndexSettingsService luceneIndexSettingsService,
            LuceneAnalyzerManager luceneAnalyzerManager,
            ILuceneQueryService queryService,
            ILiquidTemplateManager liquidTemplateManager,
            ISession session,
            JavaScriptEncoder javaScriptEncoder,
            IOptions<TemplateOptions> templateOptions)
        {
            _luceneIndexProvider = luceneIndexProvider;
            _luceneIndexSettingsService = luceneIndexSettingsService;
            _luceneAnalyzerManager = luceneAnalyzerManager;
            _queryService = queryService;
            _liquidTemplateManager = liquidTemplateManager;
            _session = session;
            _javaScriptEncoder = javaScriptEncoder;
            _templateOptions = templateOptions.Value;
        }

        public string Name => "Lucene";

        public Query Create()
        {
            return new LuceneQuery();
        }

        public async Task<IQueryResults> ExecuteQueryAsync(Query query, IDictionary<string, object> parameters)
        {
            var luceneQuery = query as LuceneQuery;
            var luceneQueryResults = new LuceneQueryResults();

            await _luceneIndexProvider.SearchAsync(luceneQuery.Index, async searcher =>
            {
                var tokenizedContent = await _liquidTemplateManager.RenderStringAsync(luceneQuery.Template, _javaScriptEncoder, parameters.Select(x => new KeyValuePair<string, FluidValue>(x.Key, FluidValue.Create(x.Value, _templateOptions))));

                var parameterizedQuery = JObject.Parse(tokenizedContent);

                var analyzer = _luceneAnalyzerManager.CreateAnalyzer(await _luceneIndexSettingsService.GetIndexAnalyzerAsync(luceneQuery.Index));
                var context = new LuceneQueryContext(searcher, LuceneSettings.DefaultVersion, analyzer);
                var docs = await _queryService.SearchAsync(context, parameterizedQuery);
                luceneQueryResults.Count = docs.Count;

                if (luceneQuery.ReturnContentItems)
                {
                    // We always return an empty collection if the bottom lines queries have no results.
                    luceneQueryResults.Items = new List<ContentItem>();

                    // Load corresponding content item versions
                    var indexedContentItemVersionIds = docs.TopDocs.ScoreDocs.Select(x => searcher.Doc(x.Doc).Get("Content.ContentItem.ContentItemVersionId")).ToArray();
                    var dbContentItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentItemVersionId.IsIn(indexedContentItemVersionIds)).ListAsync();

                    // Reorder the result to preserve the one from the lucene query
                    if (dbContentItems.Any())
                    {
                        var dbContentItemVersionIds = dbContentItems.ToDictionary(x => x.ContentItemVersionId, x => x);
                        var indexedAndInDB = indexedContentItemVersionIds.Where(dbContentItemVersionIds.ContainsKey);
                        luceneQueryResults.Items = indexedAndInDB.Select(x => dbContentItemVersionIds[x]).ToArray();
                    }
                }
                else
                {
                    var results = new List<JObject>();
                    foreach (var document in docs.TopDocs.ScoreDocs.Select(hit => searcher.Doc(hit.Doc)))
                    {
                        // need to group by field name and output jproperty(s), depending on count of values,
                        // with either 1) a singular value or 2) ienumerable list of values
                        var jprops = document.GroupBy( f => f.Name, f => f.GetStringValue() )
                                             .Select( g => g.Count() == 1 ? new JProperty( g.Key, g.First() ) : new JProperty( g.Key, g ) );

                        results.Add( new JObject( jprops ) );
                    }

                    luceneQueryResults.Items = results;
                }
            });

            return luceneQueryResults;
        }
    }
}
