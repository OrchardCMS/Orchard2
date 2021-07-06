using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Modules;
using OrchardCore.Settings;
using OrchardCore.Users.Models;

namespace OrchardCore.Users.Drivers
{
    [Feature("OrchardCore.Users.Registration")]
    public class RegistrationSettingsDisplayDriver : SectionDisplayDriver<ISite, RegistrationSettings>
    {
        public const string GroupId = "userRegistration";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public RegistrationSettingsDisplayDriver(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }
        public override async Task<IDisplayResult> EditAsync(RegistrationSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageUsers))
            {
                return null;
            }

            return Initialize<RegistrationSettings>("RegistrationSettings_Edit", model =>
            {
                model.UsersCanRegister = settings.UsersCanRegister;
                model.UsersMustValidateEmail = settings.UsersMustValidateEmail;
                model.ConfirmPasswordRequired = settings.ConfirmPasswordRequired;
                model.UseSiteTheme = settings.UseSiteTheme;
                model.NoPasswordForExternalUsers = settings.NoPasswordForExternalUsers;
                model.NoUsernameForExternalUsers = settings.NoUsernameForExternalUsers;
                model.NoEmailForExternalUsers = settings.NoEmailForExternalUsers;
                model.UseScriptToGenerateUsername = settings.UseScriptToGenerateUsername;
                model.GenerateUsernameScript = settings.GenerateUsernameScript;
            }).Location("Content:5").OnGroup(GroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(RegistrationSettings section, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageUsers))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                await context.Updater.TryUpdateModelAsync(section, Prefix);
            }

            return await EditAsync(section, context);
        }
    }
}
