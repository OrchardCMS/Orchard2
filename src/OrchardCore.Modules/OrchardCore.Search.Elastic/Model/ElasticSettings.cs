using OrchardCore.Contents.Indexing;

namespace OrchardCore.Search.Elastic.Model
{
    public class ElasticSettings
    {
        public static readonly string[] FullTextField = new string[] { IndexingConstants.FullTextKey };

        public static string StandardAnalyzer = "standardanalyzer";

        public string SearchIndex { get; set; }

        public string[] DefaultSearchFields { get; set; } = FullTextField;

        public bool AllowElasticQueriesInSearch { get; set; } = false;
    }
}
