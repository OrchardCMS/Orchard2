using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Setup.Core
{
    public class UrlService : IUrlService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UrlService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string GetEncodedUrl(ShellSettings shellSettings, Dictionary<string, string> queryParams = null)
        {
            string baseUrl = string.Empty;           
            var httpContext = _httpContextAccessor.HttpContext;

            var tenantUrlHost = shellSettings?.RequestUrlHost?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var hostString = tenantUrlHost != null ? new HostString(tenantUrlHost) : httpContext.Request.Host;

            var pathString = httpContext.Features.Get<ShellContextFeature>().OriginalPathBase;
            if (!String.IsNullOrEmpty(shellSettings.RequestUrlPrefix))
            {
                pathString = pathString.Add('/' + shellSettings.RequestUrlPrefix);
            }

            baseUrl = $"{httpContext.Request.Scheme}://{hostString + pathString}";

            if (queryParams != null)
            {
                var queryString = QueryString.Create(queryParams);
                baseUrl += queryString;
            }

            return baseUrl;
        }

        public string GetDisplayUrl(ShellSettings shellSettings)
        {
            string baseUrl = string.Empty;
            
            var httpContext = _httpContextAccessor.HttpContext;

            var tenantUrlHost = shellSettings?.RequestUrlHost?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var hostString = tenantUrlHost != null ? new HostString(tenantUrlHost) : httpContext.Request.Host;

            var pathString = httpContext.Features.Get<ShellContextFeature>().OriginalPathBase;
            if (!String.IsNullOrEmpty(shellSettings.RequestUrlPrefix))
            {
                pathString = pathString.Add('/' + shellSettings.RequestUrlPrefix);
            }

            baseUrl = $"{httpContext.Request.Scheme}://{hostString.Value + pathString.Value}";           

            return baseUrl;
        }
    }
}
