using System.Collections.Generic;
using Newtonsoft.Json;
using OrchardCore.Data.Documents;

namespace OrchardCore.Search.Elastic.Model
{
    public class ElasticIndexSettings
    {
        [JsonIgnore]
        public string IndexName { get; set; }

        public string AnalyzerName { get; set; }

        public bool IndexLatest { get; set; }

        public string[] IndexedContentTypes { get; set; }

        public string Culture { get; set; }
    }

    public class ElasticIndexSettingsDocument : Document
    {
        public Dictionary<string, ElasticIndexSettings> ElasticIndexSettings { get; set; } = new Dictionary<string, ElasticIndexSettings>();
    }
}
