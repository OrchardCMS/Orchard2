using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Google.Analytics.Settings;
using OrchardCore.Google.Analytics.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.Google.Analytics.Drivers
{
    public class GoogleAnalyticsSettingsDisplayDriver : SectionDisplayDriver<ISite, GoogleAnalyticsSettings>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GoogleAnalyticsSettingsDisplayDriver(
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IDisplayResult> EditAsync(GoogleAnalyticsSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageGoogleAnalytics))
            {
                return null;
            }

            return Initialize<GoogleAnalyticsSettingsViewModel>("GoogleAnalyticsSettings_Edit", model =>
            {
                model.SettingEntries = settings.SettingEntries;
            }).Location("Content:5").OnGroup(GoogleConstants.Features.GoogleAnalytics);
        }

        public override async Task<IDisplayResult> UpdateAsync(GoogleAnalyticsSettings settings, BuildEditorContext context)
        {
            if (context.GroupId == GoogleConstants.Features.GoogleAnalytics)
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageGoogleAnalytics))
                {
                    return null;
                }

                var model = new GoogleAnalyticsSettingsViewModel();
                await context.Updater.TryUpdateModelAsync(model, Prefix);

                if (context.Updater.ModelState.IsValid)
                {
                    settings.SettingEntries = model.SettingEntries;
                }
            }

            return await EditAsync(settings, context);
        }
    }
}
