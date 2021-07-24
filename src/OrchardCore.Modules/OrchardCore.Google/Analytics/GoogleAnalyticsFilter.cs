using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrchardCore.Admin;
using OrchardCore.Entities;
using OrchardCore.Google.Analytics.Settings;
using OrchardCore.ResourceManagement;
using OrchardCore.Settings;
using System.Linq;
using System.Text;

namespace OrchardCore.Google.Analytics
{
    public class GoogleAnalyticsFilter : IAsyncResultFilter
    {
        private readonly IResourceManager _resourceManager;
        private readonly ISiteService _siteService;

        private HtmlString _scriptsCache;

        public GoogleAnalyticsFilter(
            IResourceManager resourceManager,
            ISiteService siteService)
        {
            _resourceManager = resourceManager;
            _siteService = siteService;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Should only run on the front-end for a full view
            if ((context.Result is ViewResult || context.Result is PageResult) &&
                !AdminAttribute.IsApplied(context.HttpContext))
            {
                if (_scriptsCache == null)
                {
                    var settings = (await _siteService.GetSiteSettingsAsync()).As<GoogleAnalyticsSettings>();

                    if (settings.SettingEntries.Length > 0)
                    {
                        var defaultMeasurementId = settings.SettingEntries.FirstOrDefault(x => x.IsDefault == true).MeasurementId;
                        var sb = new StringBuilder($"<!-- Global site tag (gtag.js) - Google Analytics -->\n<script async src=\"https://www.googletagmanager.com/gtag/js?id={defaultMeasurementId}\"></script>\n<script>window.dataLayer = window.dataLayer || [];function gtag() {{ dataLayer.push(arguments); }}gtag('js', new Date());\n");

                        foreach(SettingEntry config in settings.SettingEntries)
                        {
                            if (!string.IsNullOrWhiteSpace(config?.MeasurementId))
                            {
                                sb.Append($"gtag('config', '{config.MeasurementId}')\n");
                            }
                        }

                        sb.Append("</script>\n<!-- End Global site tag (gtag.js) - Google Analytics -->");
                        _scriptsCache = new HtmlString(sb.ToString());
                    }
                }

                if (_scriptsCache != null)
                {
                    _resourceManager.RegisterHeadScript(_scriptsCache);
                }
            }

            await next.Invoke();
        }
    }
}
