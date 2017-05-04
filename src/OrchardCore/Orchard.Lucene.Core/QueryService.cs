using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace Orchard.Lucene
{
    public class QueryService : IQueryService
    {
        private readonly IEnumerable<ILuceneQueryProvider> _queryProviders;

        public QueryService(IEnumerable<ILuceneQueryProvider> queryProviders)
        {
            _queryProviders = queryProviders;
        }

        public TopDocs Search(LuceneQueryContext context, JObject queryObj)
        {
            var queryProp = queryObj["query"] as JObject;

            if (queryProp == null)
            {
                throw new ArgumentException("Query DSL requires a [query] property");
            }

            Query query = CreateQueryFragment(context, queryProp);

            var sortProperty = queryObj["sort"];
            var fromProperty = queryObj["from"];
            var sizeProperty = queryObj["size"];

            var size = sizeProperty?.Value<int>() ?? 50;
            var from = fromProperty?.Value<int>() ?? 0;

            string sortField = null;
            string sortOrder = null;

            if (sortProperty != null)
            {
                if (sortProperty.Type == JTokenType.String)
                {
                    sortField = sortProperty.ToString();
                }
                else if (sortProperty.Type == JTokenType.Object)
                {
                    sortField = ((JProperty)sortProperty.First).Name;
                    sortOrder = ((JProperty)sortProperty.First).Value["order"].ToString();
                }
            }

            TopDocs docs = context.IndexSearcher.Search(
                query, 
                size + from,
                sortField == null ? Sort.RELEVANCE : new Sort(new SortField(sortField, SortField.Type_e.STRING, sortOrder == "desc"))
            );

            if (from > 0)
            {
                docs = new TopDocs(docs.TotalHits - from, docs.ScoreDocs.Skip(from).ToArray(), docs.MaxScore);
            }

            return docs;
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

            if (String.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            var result = new List<string>();
            using (var tokenStream = analyzer.TokenStream(fieldName, text))
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
