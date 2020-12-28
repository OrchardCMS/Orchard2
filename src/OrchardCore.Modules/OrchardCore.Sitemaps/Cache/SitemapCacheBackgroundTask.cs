using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.BackgroundTasks;
using OrchardCore.Sitemaps.Services;

namespace OrchardCore.Sitemaps.Cache
{
    [BackgroundTask(Schedule = "* * * * *", Description = "Cleanup sitemap cached files.")]
    public class SitemapCacheBackgroundTask : IBackgroundTask
    {
        public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var sitemapManager = serviceProvider.GetRequiredService<ISitemapManager>();
            var sitemapCacheProvider = serviceProvider.GetRequiredService<ISitemapCacheProvider>();

            var sitemaps = await sitemapManager.GetSitemapsAsync();
            await sitemapCacheProvider.CleanupAsync(sitemaps);
        }
    }
}
