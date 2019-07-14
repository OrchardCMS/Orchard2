using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Settings;

namespace OrchardCore.DisplayManagement.Razor
{
    /// <summary>
    /// Inject an instance of <see cref="ISite"/> in the HttpContext items such that
    /// a View can reuse it when it's executed.
    /// </summary>
    public class SiteViewResultFilter : IAsyncResultFilter, IViewResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await OnResultExecutionAsync(context.HttpContext);
            await next();
        }

        // Used when we create fake view and action contexts.
        public Task OnResultExecutionAsync(ActionContext context)
        {
            return OnResultExecutionAsync(context.HttpContext);
        }

        private async Task OnResultExecutionAsync(HttpContext context)
        {
            if (!context.Items.ContainsKey(typeof(ISite)))
            {
                var siteService = context.RequestServices.GetService<ISiteService>();

                // siteService can be null during Setup
                if (siteService != null)
                {
                    context.Items.Add(typeof(ISite), await siteService.GetSiteSettingsAsync());
                }
            }
        }
    }
}
