using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Tenants.Utilities
{
    public static class TenantHelperExtensions
    {
        public static string GetEncodedUrl(ShellSettings shellSettings, HttpRequest request, string token)
        {
            var requestHost = request.Host;
            var host = shellSettings.RequestUrlHost?.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? requestHost.Host;

            var port = requestHost.Port;

            if (port.HasValue)
            {
                host += ":" + port;
            }

            var hostString = new HostString(host);

            var pathString = request.HttpContext.Features.Get<ShellContextFeature>().OriginalPathBase;

            if (!string.IsNullOrEmpty(shellSettings.RequestUrlPrefix))
            {
                pathString = pathString.Add('/' + shellSettings.RequestUrlPrefix);
            }

            QueryString queryString;

            if (!string.IsNullOrEmpty(token))
            {
                queryString = QueryString.Create("token", token);
            }

            return $"{request.Scheme}://{hostString + pathString + queryString}";
        }
    }
}
