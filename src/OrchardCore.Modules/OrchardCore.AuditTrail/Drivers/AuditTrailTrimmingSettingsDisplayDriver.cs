using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.AuditTrail.Permissions;
using OrchardCore.AuditTrail.Settings;
using OrchardCore.AuditTrail.ViewModels;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;

namespace OrchardCore.AuditTrail.Drivers
{
    public class AuditTrailTrimmingSettingsDisplayDriver : SectionDisplayDriver<ISite, AuditTrailTrimmingSettings>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public AuditTrailTrimmingSettingsDisplayDriver(IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(AuditTrailTrimmingSettings section, BuildEditorContext context) =>
            !await IsAuthorizedToManageAuditTrailSettingsAsync()
                ? null
                : Initialize<AuditTrailTrimmingSettingsViewModel>("AuditTrailTrimmingSettings_Edit", model =>
                {
                    model.RetentionPeriodDays = section.RetentionPeriodDays;
                    model.LastRunUtc = section.LastRunUtc;
                    model.Disabled = section.Disabled;
                }).Location("Content:10").OnGroup(AuditTrailSettingsDisplayDriver.AuditTrailSettingsGroupId);

        public override async Task<IDisplayResult> UpdateAsync(AuditTrailTrimmingSettings section, BuildEditorContext context)
        {
            if (context.GroupId == AuditTrailSettingsDisplayDriver.AuditTrailSettingsGroupId)
            {
                if (!await IsAuthorizedToManageAuditTrailSettingsAsync())
                {
                    return null;
                }

                var model = new AuditTrailTrimmingSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                section.RetentionPeriodDays = model.RetentionPeriodDays;
                section.Disabled = model.Disabled;
            }

            return await EditAsync(section, context);
        }

        private Task<bool> IsAuthorizedToManageAuditTrailSettingsAsync() =>
             _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, AuditTrailPermissions.ManageAuditTrailSettings);
    }
}
