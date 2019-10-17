using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Queries
{
    public interface IQuerySource
    {
        string Name { get; }
        Query Create();
        Task<IQueryResult> ExecuteQueryAsync(Query query, IDictionary<string, object> parameters);
    }
}
