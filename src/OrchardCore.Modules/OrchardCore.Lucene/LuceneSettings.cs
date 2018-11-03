using System.Collections.Generic;
using Lucene.Net.Util;

namespace OrchardCore.Lucene
{
    public class LuceneSettings
    {       
        public string SearchIndex { get; set; }

        public string[] DefaultSearchFields { get; set; } = new string[0];

        /// <summary>
        /// Gets the list of indices and their settings.
        /// </summary>
        public Dictionary<string, IndexSettings> IndexSettings { get; } = new Dictionary<string, IndexSettings>();
    }

    public class IndexSettings
    {
        public string Name { get; set; }
        public string Analyzer { get; set; }
    }

    public class LuceneAnalyzerSettings
    {
        public string Analyzer { get; set; } = "standardanalyzer";

        public LuceneVersion Version { get; set; } = LuceneVersion.LUCENE_48;
    }
    public class DefaultLuceneAnalyzerSettings
    {
        public static string StandardAnalyzer = "standardanalyzer";

        public static LuceneVersion DefaultVersion = LuceneVersion.LUCENE_48;
    }
}
