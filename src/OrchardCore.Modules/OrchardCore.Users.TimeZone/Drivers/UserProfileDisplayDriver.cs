using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Entities.DisplayManagement;
using OrchardCore.Modules;
using OrchardCore.Users.Models;
using OrchardCore.Users.TimeZone.Models;
using OrchardCore.Users.TimeZone.Services;
using OrchardCore.Users.TimeZone.ViewModels;

namespace OrchardCore.Users.TimeZone.Drivers
{
    public class UserProfileDisplayDriver : SectionDisplayDriver<User, UserProfile>
    {
        private readonly IClock _clock;
        private readonly IUserTimeZoneService _userTimeZoneService;

        public UserProfileDisplayDriver(
            IClock clock,
            IUserTimeZoneService userTimeZoneService) {
            _clock = clock;
            _userTimeZoneService = userTimeZoneService;
        }

        public override IDisplayResult Edit(UserProfile profile, BuildEditorContext context)
        {
            return Initialize<UserProfileViewModel>("UserProfile_Edit", model =>
            {
                model.TimeZone = profile.TimeZone;
                model.TimeZones = _clock.GetTimeZones();
            }).Location("Content:2");
        }

        public override async Task<IDisplayResult> UpdateAsync(UserProfile profile, IUpdateModel updater, BuildEditorContext context)
        {
            var model = new UserProfileViewModel();

            if (await context.Updater.TryUpdateModelAsync(model, Prefix))
            {
                profile.TimeZone = model.TimeZone;
            }

            //await _userTimeZoneService.SetSiteTimeZoneAsync(profile.TimeZone);
            
            return Edit(profile);
        }
    }
}
