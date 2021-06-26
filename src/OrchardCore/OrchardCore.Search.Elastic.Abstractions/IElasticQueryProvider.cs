using Nest;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Search.Elastic
{
    public interface IElasticQueryProvider
    {
        IQuery CreateQuery(IElasticQueryService builder, ElasticQueryContext context, string type, JObject query);
    }
}
