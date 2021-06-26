using OrchardCore.Queries;

namespace OrchardCore.Search.Elastic
{
    public class ElasticQuery : Query
    {
        public ElasticQuery() : base("Elastic")
        {
        }

        public string Index { get; set; }
        public string Template { get; set; }
        public bool ReturnContentItems { get; set; }
    }
}
