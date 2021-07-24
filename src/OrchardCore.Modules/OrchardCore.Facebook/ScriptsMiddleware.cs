using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.Entities;
using OrchardCore.Facebook.Settings;
using OrchardCore.Settings;

namespace OrchardCore.Facebook
{
    public class ScriptsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISiteService _siteService;

        public ScriptsMiddleware(RequestDelegate next, ISiteService siteService)
        {
            _next = next;
            _siteService = siteService;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/OrchardCore.Facebook/sdk", StringComparison.OrdinalIgnoreCase))
            {
                var script = default(string);
                var site = (await _siteService.GetSiteSettingsAsync());
                var settings = site.As<FacebookSettings>();
                if (Path.GetFileName(httpContext.Request.Path.Value) == "fbsdk.js")
                {
                    var locale = CultureInfo.CurrentUICulture.Name.Replace('-', '_');
                    script = $@"(function(d){{
                        var js, id = 'facebook-jssdk'; if (d.getElementById(id)) {{ return; }}
                        js = d.createElement('script'); js.id = id; js.async = true;
                        js.src = ""https://connect.facebook.net/{locale}/{settings.SdkJs}"";
                        d.getElementsByTagName('head')[0].appendChild(js);
                    }} (document));";
                }
                else if (Path.GetFileName(httpContext.Request.Path.Value) == "fb.js")
                {
                    if (!string.IsNullOrWhiteSpace(settings?.AppId))
                    {
                        var options = $"{{ appId:'{settings.AppId}',version:'{settings.Version}'";
                        if (string.IsNullOrWhiteSpace(settings.FBInitParams))
                        {
                            options = string.Concat(options, "}");
                        }
                        else
                        {
                            options = string.Concat(options, ",", settings.FBInitParams, "}");
                        }
                        script = $"window.fbAsyncInit = function(){{ FB.init({options});}};";
                    }
                }

                if (script != null)
                {
                    var bytes = Encoding.UTF8.GetBytes(script);
                    var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;
                    await httpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(script), 0, bytes.Length, cancellationToken);
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
