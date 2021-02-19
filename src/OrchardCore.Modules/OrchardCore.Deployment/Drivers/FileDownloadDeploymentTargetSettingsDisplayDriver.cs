using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.Deployment.Models;
using OrchardCore.Deployment.ViewModels;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;

namespace OrchardCore.Deployment.Drivers
{
    public class FileDownloadDeploymentTargetSettingsDisplayDriver : SectionDisplayDriver<ISite, FileDownloadDeploymentTargetSettings>
    {
        private const string SettingsGroupId = "FileDownloadDeploymentTarget";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public FileDownloadDeploymentTargetSettingsDisplayDriver(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(FileDownloadDeploymentTargetSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !await _authorizationService.AuthorizeAsync(user, OrchardCore.Deployment.CommonPermissions.ManageDeploymentPlan))
            {
                return null;
            }

            return Initialize<FileDownloadDeploymentTargetSettingsViewModel>("FileDownloadDeploymentTargetSettings_Edit", model =>
            {
                model.RsaEncryptionSecret = settings.RsaEncryptionSecret;
                model.RsaSigningSecret = settings.RsaSigningSecret;
            }).Location("Content:2").OnGroup(SettingsGroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(FileDownloadDeploymentTargetSettings settings, BuildEditorContext context)
        {
            if (context.GroupId == SettingsGroupId)
            {
                var model = new FileDownloadDeploymentTargetSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                settings.RsaEncryptionSecret = model.RsaEncryptionSecret;
                settings.RsaSigningSecret = model.RsaSigningSecret;
            }

            return await EditAsync(settings, context);
        }
    }
}
