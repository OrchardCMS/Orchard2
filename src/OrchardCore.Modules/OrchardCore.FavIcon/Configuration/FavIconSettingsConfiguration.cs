using Microsoft.Extensions.Options;
using OrchardCore.Entities;
using OrchardCore.Settings;

namespace OrchardCore.FavIcon.Configuration
{
    public class FavIconSettingsConfiguration : IConfigureOptions<FavIconSettings>
    {
        private readonly ISiteService _site;

        public FavIconSettingsConfiguration(ISiteService site) => _site = site;

        public void Configure(FavIconSettings options)
        {
            var settings = _site.GetSiteSettingsAsync()
                .GetAwaiter().GetResult()
                .As<FavIconSettings>();

            options.MediaLibraryFolder = settings.MediaLibraryFolder;
            options.TileColor = settings.TileColor;
            options.ThemeColor = settings.ThemeColor;
        }
    }
}
