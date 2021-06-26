using Nest;

namespace OrchardCore.Search.Elastic
{
    public class ElasticQueryContext
    {
        public ElasticQueryContext()
        {
            
        }

        public ElasticQueryContext(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; set; }

        /// <summary>
        /// The Analyzer later needs to be implemented from a list of Elastic Analyzers
        /// </summary>
        public IAnalyzer DefaultAnalyzer { get; }

        //We may need to have configurable index searcher
        //public IndexSearcher IndexSearcher { get; set; }
    }
}
