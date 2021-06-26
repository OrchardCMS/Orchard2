using System.Collections.Generic;

namespace OrchardCore.Search.Elastic
{
    public class ElasticTopDocs
    {
        public List<Dictionary<string,object>> TopDocs { get; set; }
        public int Count { get; set; }
    }
}
