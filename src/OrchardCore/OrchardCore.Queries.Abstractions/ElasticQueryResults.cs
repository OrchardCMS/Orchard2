using System.Collections.Generic;

namespace OrchardCore.Queries
{
    public class ElasticQueryResults : IQueryResults
    {
        public IEnumerable<object> Items { get; set; }
        public int Count { get; set; }
    }
}
