using System.Threading.Tasks;
using Nest;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Search.Elastic
{
    public interface IElasticQueryService
    {
        Task<ElasticTopDocs> SearchAsync(ElasticQueryContext context, JObject queryObj);
        IQuery CreateQueryFragment(ElasticQueryContext context, JObject queryObj);
    }
}
