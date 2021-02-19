using System;
using System.Threading.Tasks;

namespace OrchardCore.Workflows.Http.Services
{
    public interface IHttpRequestEventSecretService
    {
        Task<string> GetUrlAsync(string httpRequestEventSecretName);
    }
}
