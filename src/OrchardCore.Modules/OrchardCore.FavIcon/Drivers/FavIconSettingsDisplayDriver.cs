using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.FavIcon.Configuration;
using OrchardCore.FavIcon.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.FavIcon.Drivers
{
    public class FavIconSettingsDisplayDriver : SectionDisplayDriver<ISite, FavIconSettings>
    {
        public const string GroupId = "favicon";
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public FavIconSettingsDisplayDriver(
            IShellHost shellHost,
            ShellSettings shellSettings,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(FavIconSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageFavIconSettings))
            {
                return null;
            }

            return Initialize<FavIconSettingsViewModel>("FavIconSettings_Edit", model =>
                {
                    model.MediaLibraryFolder = settings.MediaLibraryFolder;
                    model.TileColor = settings.TileColor;
                    model.ThemeColor = settings.ThemeColor;
                })
                .Location("Content")
                .OnGroup(GroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(FavIconSettings section, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageFavIconSettings))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var model = new FavIconSettingsViewModel();

                if (await context.Updater.TryUpdateModelAsync(model, Prefix))
                {
                    section.MediaLibraryFolder = model.MediaLibraryFolder?.Trim();
                    section.TileColor = model.TileColor?.Trim();
                    section.ThemeColor = model.ThemeColor?.Trim();

                    // Release the tenant to apply settings.
                    await _shellHost.ReleaseShellContextAsync(_shellSettings);
                }
            }

            return await EditAsync(section, context);
        }
    }
}
