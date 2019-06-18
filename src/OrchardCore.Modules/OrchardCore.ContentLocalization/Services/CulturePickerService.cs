using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.Autoroute.Services;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.Settings;

namespace OrchardCore.ContentLocalization.Services
{
    public class ContentCulturePickerService : IContentCulturePickerService
    {
        private readonly YesSql.ISession _session;
        private readonly IAutorouteEntries _autorouteEntries;
        private readonly ILocalizationEntries _localizationEntries;
        private readonly ISiteService _siteService;
        private readonly IContentLocalizationManager _contentLocalizationManager;


        public ContentCulturePickerService(
            IContentLocalizationManager contentLocalizationManager,
            YesSql.ISession session,
            IAutorouteEntries autorouteEntries,
            ILocalizationEntries localizationEntries,
            ISiteService siteService)
        {
            _contentLocalizationManager = contentLocalizationManager;
            _session = session;
            _autorouteEntries = autorouteEntries;
            _localizationEntries = localizationEntries;
            _siteService = siteService;
        }

        public async Task<string> GetContentItemIdFromRouteAsync(PathString url)
        {
            if (!url.HasValue)
            {
                url = "/";
            }

            string contentItemId;

            if (url == "/")
            {
                // get contentItemId from homeroute
                var siteSettings = await _siteService.GetSiteSettingsAsync();
                contentItemId = siteSettings.HomeRoute["contentItemId"]?.ToString();

            }
            else
            {
                // try to get from autorouteEntries
                _autorouteEntries.TryGetContentItemId(url, out contentItemId);
            }

            if (string.IsNullOrEmpty(contentItemId))
            {
                return null;
            }

            return contentItemId;
        }

        public async Task<LocalizationEntry> GetLocalizationFromRouteAsync(PathString url)
        {
            var contentItemId = await GetContentItemIdFromRouteAsync(url);

            if (!string.IsNullOrEmpty(contentItemId))
            {
                if (_localizationEntries.TryGetLocalization(contentItemId, out var localization))
                {
                    return localization;
                }
            }

            return null;
        }
    }
}
