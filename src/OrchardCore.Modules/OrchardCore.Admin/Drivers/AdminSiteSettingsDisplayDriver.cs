using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.Admin.Models;
using OrchardCore.Admin.ViewModels;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;

namespace OrchardCore.Admin.Drivers
{
    public class AdminSiteSettingsDisplayDriver : SectionDisplayDriver<ISite, AdminSettings>
    {
        public const string GroupId = "admin";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public AdminSiteSettingsDisplayDriver(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(AdminSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, PermissionsAdminSettings.ManageAdminSettings))
            {
                return null;
            }

            return Initialize<AdminSettingsViewModel>("AdminSettings_Edit", model =>
            {
                model.DisplayDarkMode = settings.DisplayDarkMode;
                model.DisplayMenuFilter = settings.DisplayMenuFilter;
                model.DisplayNewMenu = settings.DisplayNewMenu;
                }).Location("Content:3").OnGroup(GroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(AdminSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, PermissionsAdminSettings.ManageAdminSettings))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var model = new AdminSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                settings.DisplayDarkMode = model.DisplayDarkMode;
                settings.DisplayMenuFilter = model.DisplayMenuFilter;
                settings.DisplayNewMenu = model.DisplayNewMenu;
            }

            return await EditAsync(settings, context);
        }
    }
}
