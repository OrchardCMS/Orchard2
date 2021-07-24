using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using OrchardCore.Lucene.FieldComparers;

namespace OrchardCore.Lucene
{
    public class LuceneQueryService : ILuceneQueryService
    {
        private readonly IEnumerable<ILuceneQueryProvider> _queryProviders;

        public LuceneQueryService(IEnumerable<ILuceneQueryProvider> queryProviders)
        {
            _queryProviders = queryProviders;
        }

        public Task<LuceneTopDocs> SearchAsync(LuceneQueryContext context, JObject queryObj)
        {
            var queryProp = queryObj["query"] as JObject;

            if (queryProp == null)
            {
                throw new ArgumentException("Query DSL requires a [query] property");
            }

            var query = CreateQueryFragment(context, queryProp);

            var sortProperty = queryObj["sort"];
            var fromProperty = queryObj["from"];
            var sizeProperty = queryObj["size"];

            var size = sizeProperty?.Value<int>() ?? 50;
            var from = fromProperty?.Value<int>() ?? 0;

            var sortFields = GetSortFields(sortProperty);

            LuceneTopDocs result = null;
            TopDocs topDocs = null;

            if (size > 0)
            {
                topDocs = context.IndexSearcher.Search(
                    query,
                    size + from,
                    sortFields.Length == 0 ? Sort.RELEVANCE : new Sort(sortFields)
                );

                if (from > 0)
                {
                    topDocs = new TopDocs(topDocs.TotalHits - from, topDocs.ScoreDocs.Skip(from).ToArray(), topDocs.MaxScore);
                }

                var collector = new TotalHitCountCollector();
                context.IndexSearcher.Search(query, collector);

                result = new LuceneTopDocs() { TopDocs = topDocs, Count = collector.TotalHits };
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Returns zero or more SortFields from sortProperty
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static SortField[] GetSortFields(JToken item)
        {
            if (item == null) return new SortField[0];

            switch (item.Type)
            {
                case JTokenType.Array:
                    return item.Children().Select(GetSortField).ToArray();
                case JTokenType.String:
                case JTokenType.Object:
                    return new[] { GetSortField(item) };
                default:
                    return new SortField[0];
            }
        }

        /// <summary>
        /// Get sort field based on token types string and object
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static SortField GetSortField(JToken item)
        {
            if (item.Type == JTokenType.String)
            {
                return new SortField((string)item, SortFieldType.STRING);
            }

            var itemFirst = (JProperty) item.First;
            var sortField = itemFirst.Name;
            var sortOrder = itemFirst.Value["order"]?.ToString();
            var sortType = itemFirst.Value["type"]?.ToString();

            if (sortOrder == "random")
            {
                return new SortField(sortField, new RandomFieldComparatorSource());
            }

            if (sortType == null || !Enum.TryParse(sortType, true, out SortFieldType sortFieldType))
            {
                sortFieldType = SortFieldType.STRING;
            }

            return new SortField(sortField, sortFieldType, sortOrder == "desc");
        }

        public Query CreateQueryFragment(LuceneQueryContext context, JObject queryObj)
        {
            var first = queryObj.Properties().First();

            Query query = null;

            foreach (var queryProvider in _queryProviders)
            {
                query = queryProvider.CreateQuery(this, context, first.Name, (JObject)first.Value);

                if (query != null)
                {
                    break;
                }
            }

            return query;
        }

        public static List<string> Tokenize(string fieldName, string text, Analyzer analyzer)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            var result = new List<string>();
            using (var tokenStream = analyzer.GetTokenStream(fieldName, text))
            {
                tokenStream.Reset();
                while (tokenStream.IncrementToken())
                {
                    var termAttribute = tokenStream.GetAttribute<ICharTermAttribute>();

                    if (termAttribute != null)
                    {
                        result.Add(termAttribute.ToString());
                    }
                }
            }

            return result;
        }
    }
}
