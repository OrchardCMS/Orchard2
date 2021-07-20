using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Profile.ViewModels;

namespace OrchardCore.Profile.Controllers
{
    [Authorize]
    public class ProfileController : Controller, IUpdateModel
    {
        private readonly IDisplayManager<IProfile> _profileDisplayManager;
        private readonly IProfileService _profileService;
        private readonly INotifier _notifier;
        private readonly IAuthorizationService _authorizationService;

        private readonly IContentDefinitionManager _contentDefinitionManager;

        public ProfileController(
            IProfileService profileService,
            IDisplayManager<IProfile> profileDisplayManager,
            IAuthorizationService authorizationService,
            INotifier notifier,
            IHtmlLocalizer<ProfileController> h,
            IStringLocalizer<ProfileController> s,
            IContentDefinitionManager contentDefinitionManager
            )
        {
            _profileDisplayManager = profileDisplayManager;
            _profileService = profileService;
            _notifier = notifier;
            _authorizationService = authorizationService;
            H = h;
            S = s;
            _contentDefinitionManager = contentDefinitionManager;
        }

        IHtmlLocalizer H { get; set; }
        IStringLocalizer S { get; set; }


        public async Task<IActionResult> Index(string groupId = "")
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                groupId = "general";
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageGroupProfile, (object)groupId))
            {
                return Unauthorized();
            }

            var profile = await _profileService.GetProfileAsync();
            if (profile.UserName != User.Identity.Name)
            {
                profile.UserName = User.Identity.Name;
                await _profileService.UpdateProfileAsync(profile);
            }

            var viewModel = new ProfileIndexViewModel
            {
                GroupId = groupId,
                Shape = await _profileDisplayManager.BuildEditorAsync(profile, this, false, groupId)
            };

            return View(viewModel);
        }

    }
}
