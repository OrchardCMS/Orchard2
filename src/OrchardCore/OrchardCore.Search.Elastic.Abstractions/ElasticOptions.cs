using System.Collections.Generic;

namespace OrchardCore.Search.Elastic
{
    public class ElasticOptions
    {
        public IList<IElasticAnalyzer> Analyzers { get; } = new List<IElasticAnalyzer>();
    }
}
