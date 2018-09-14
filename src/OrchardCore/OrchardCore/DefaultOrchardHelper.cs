using Microsoft.AspNetCore.Http;

namespace OrchardCore
{
    public class DefaultOrchardHelper : IOrchardHelper
    {
        public DefaultOrchardHelper(IHttpContextAccessor httpContextAccessor)
        {
            HttpContext = httpContextAccessor.HttpContext;
        }

        public HttpContext HttpContext { get; set; }
    }
}
