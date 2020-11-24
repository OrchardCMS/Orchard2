using System;

namespace OrchardCore.Sitemaps.Models
{
    /// <summary>
    /// A sitemap source for managing custom url.
    /// </summary>
    public class CustomUrlSitemapSource : SitemapSource
    {
        /// <summary>
        /// the custom url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// last update. Updated automatically by te system.
        /// </summary>
        public DateTime? LastUpdate { get; set; } = DateTime.Now;

        /// <summary>
        /// Change frequency to apply to sitemap entries.
        /// </summary>
        public ChangeFrequency ChangeFrequency { get; set; }

        // Handle as int, and convert to float, when building, to support localization.
        public int Priority { get; set; } = 5;
    }
}
