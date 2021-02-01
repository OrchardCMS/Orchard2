using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using OrchardCore.Users.Indexes;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Users.ViewModels;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Users.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<IUser> _userManager;
        private readonly SignInManager<IUser> _signInManager;
        private readonly ISession _session;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISiteService _siteService;
        private readonly IDisplayManager<User> _userDisplayManager;
        private readonly INotifier _notifier;
        private readonly IUserService _userService;
        private readonly IUpdateModelAccessor _updateModelAccessor;

        private readonly dynamic New;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;

        public AdminController(
            IDisplayManager<User> userDisplayManager,
            SignInManager<IUser> signInManager,
            IAuthorizationService authorizationService,
            ISession session,
            UserManager<IUser> userManager,
            IUserService userService,
            INotifier notifier,
            ISiteService siteService,
            IShapeFactory shapeFactory,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            IStringLocalizer<AdminController> stringLocalizer,
            IUpdateModelAccessor updateModelAccessor)
        {
            _userDisplayManager = userDisplayManager;
            _signInManager = signInManager;
            _authorizationService = authorizationService;
            _session = session;
            _userManager = userManager;
            _notifier = notifier;
            _siteService = siteService;
            _userService = userService;
            _updateModelAccessor = updateModelAccessor;

            New = shapeFactory;
            H = htmlLocalizer;
            S = stringLocalizer;
        }

        public async Task<ActionResult> Index(UserIndexOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);

            var users = _session.Query<User, UserIndex>();

            switch (options.Filter)
            {
                case UsersFilter.Approved:
                    //users = users.Where(u => u.RegistrationStatus == UserStatus.Approved);
                    break;
                case UsersFilter.Pending:
                    //users = users.Where(u => u.RegistrationStatus == UserStatus.Pending);
                    break;
                case UsersFilter.EmailPending:
                    //users = users.Where(u => u.EmailStatus == UserStatus.Pending);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var normalizedSearchUserName = _userManager.NormalizeName(options.Search);
                var normalizedSearchEMail = _userManager.NormalizeEmail(options.Search);

                users = users.Where(u => u.NormalizedUserName.Contains(normalizedSearchUserName) || u.NormalizedEmail.Contains(normalizedSearchEMail));
            }

            switch (options.Order)
            {
                case UsersOrder.Name:
                    users = users.OrderBy(u => u.NormalizedUserName);
                    break;
                case UsersOrder.Email:
                    users = users.OrderBy(u => u.NormalizedEmail);
                    break;
                case UsersOrder.CreatedUtc:
                    //users = users.OrderBy(u => u.CreatedUtc);
                    break;
                case UsersOrder.LastLoginUtc:
                    //users = users.OrderBy(u => u.LastLoginUtc);
                    break;
            }

            var count = await users.CountAsync();

            var results = await users
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize)
                .ListAsync();

            // Maintain previous route data when generating page links
            var routeData = new RouteData();
            routeData.Values.Add("Options.Filter", options.Filter);
            routeData.Values.Add("Options.Search", options.Search);
            routeData.Values.Add("Options.Order", options.Order);

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count).RouteData(routeData);

            var userEntries = new List<UserEntry>();

            foreach (var user in results)
            {
                userEntries.Add(new UserEntry
                {
                    UserId = user.UserId,
                    Shape = await _userDisplayManager.BuildDisplayAsync(user, updater: _updateModelAccessor.ModelUpdater, displayType: "SummaryAdmin")
                }
                );
            }

            var model = new UsersIndexViewModel
            {
                Users = userEntries,
                Options = options,
                Pager = pagerShape
            };

            model.Options.UserFilters = new List<SelectListItem>() {
                new SelectListItem() { Text = S["All"], Value = nameof(UsersFilter.All) },
                //new SelectListItem() { Text = S["Approved"], Value = nameof(UsersFilter.Approved) },
                //new SelectListItem() { Text = S["Email pending"], Value = nameof(UsersFilter.EmailPending) },
                //new SelectListItem() { Text = S["Pending"], Value = nameof(UsersFilter.Pending) }
            };

            model.Options.UserSorts = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Name"], Value = nameof(UsersOrder.Name) },
                new SelectListItem() { Text = S["Email"], Value = nameof(UsersOrder.Email) },
                //new SelectListItem() { Text = S["Created date"], Value = nameof(UsersOrder.CreatedUtc) },
                //new SelectListItem() { Text = S["Last Login date"], Value = nameof(UsersOrder.LastLoginUtc) }
            };

            model.Options.UsersBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Approve"], Value = nameof(UsersBulkAction.Approve) },
                new SelectListItem() { Text = S["Enable"], Value = nameof(UsersBulkAction.Enable) },
                new SelectListItem() { Text = S["Disable"], Value = nameof(UsersBulkAction.Disable) },
                new SelectListItem() { Text = S["Delete"], Value = nameof(UsersBulkAction.Delete) }
            };

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Filter")]
        public ActionResult IndexFilterPOST(UsersIndexViewModel model)
        {
            return RedirectToAction("Index", new RouteValueDictionary {
                { "Options.Filter", model.Options.Filter },
                { "Options.Order", model.Options.Order },
                { "Options.Search", model.Options.Search }
            });
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> IndexPOST(UserIndexOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var checkedContentItems = await _session.Query<User, UserIndex>().Where(x => x.UserId.IsIn(itemIds)).ListAsync();
                switch (options.BulkAction)
                {
                    case UsersBulkAction.None:
                        break;
                    case UsersBulkAction.Approve:
                        foreach (var item in checkedContentItems)
                        {
                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(item);
                            await _userManager.ConfirmEmailAsync(item, token);
                            _notifier.Success(H["User {0} successfully approved.", item.UserName]);
                        }
                        break;
                    case UsersBulkAction.Delete:
                        foreach (var item in checkedContentItems)
                        {
                            await _userManager.DeleteAsync(item);
                            _notifier.Success(H["User {0} successfully deleted.", item.UserName]);
                        }
                        break;
                    case UsersBulkAction.Disable:
                        foreach (var item in checkedContentItems)
                        {
                            item.IsEnabled = false;
                            await _userManager.UpdateAsync(item);
                            _notifier.Success(H["User {0} successfully disabled.", item.UserName]);
                        }
                        break;
                    case UsersBulkAction.Enable:
                        foreach (var item in checkedContentItems)
                        {
                            item.IsEnabled = true;
                            await _userManager.UpdateAsync(item);
                            _notifier.Success(H["User {0} successfully enabled.", item.UserName]);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Create()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var shape = await _userDisplayManager.BuildEditorAsync(new User(), updater: _updateModelAccessor.ModelUpdater, isNew: true);

            return View(shape);
        }

        [HttpPost]
        [ActionName(nameof(Create))]
        public async Task<IActionResult> CreatePost(string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var user = new User();

            var shape = await _userDisplayManager.UpdateEditorAsync(user, updater: _updateModelAccessor.ModelUpdater, isNew: true);

            if (!ModelState.IsValid)
            {
                return View(shape);
            }

            await _userService.CreateUserAsync(user, null, ModelState.AddModelError);

            if (!ModelState.IsValid)
            {
                return View(shape);
            }

            _notifier.Success(H["User created successfully."]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        public async Task<IActionResult> Edit(string id, string returnUrl)
        {
            // When no id is provided we assume the user is trying to edit their own profile.
            if (String.IsNullOrEmpty(id))
            {
                id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOwnUserInformation))
                {
                    return Forbid();
                }
            }
            else
            {
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
                {
                    return Forbid();
                }
            }

            var user = await _userManager.FindByIdAsync(id) as User;
            if (user == null)
            {
                return NotFound();
            }

            var shape = await _userDisplayManager.BuildEditorAsync(user, updater: _updateModelAccessor.ModelUpdater, isNew: false);

            return View(shape);
        }

        [HttpPost]
        [ActionName(nameof(Edit))]
        public async Task<IActionResult> EditPost(string id, string returnUrl)
        {
            // When no id is provided we assume the user is trying to edit their own profile.
            //var editingOwnUser = false;
            if (String.IsNullOrEmpty(id))
            {
                //editingOwnUser = true;
                id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageOwnUserInformation))
                {
                    return Forbid();
                }
            }
            else if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id) as User;
            if (user == null)
            {
                return NotFound();
            }

            var shape = await _userDisplayManager.UpdateEditorAsync(user, updater: _updateModelAccessor.ModelUpdater, isNew: false);

            if (!ModelState.IsValid)
            {
                return View(shape);
            }

            var result = await _userManager.UpdateAsync(user);

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            if (!ModelState.IsValid)
            {
                return View(shape);
            }

            if (String.Equals(User.FindFirstValue(ClaimTypes.NameIdentifier), user.UserId, StringComparison.OrdinalIgnoreCase))
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            _notifier.Success(H["User updated successfully."]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id) as User;

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _notifier.Success(H["User deleted successfully."]);
            }
            else
            {
                _session.Cancel();

                _notifier.Error(H["Could not delete the user."]);

                foreach (var error in result.Errors)
                {
                    _notifier.Error(H[error.Description]);
                }
            }

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        public async Task<IActionResult> EditPassword(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id) as User;

            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordViewModel { Email = user.Email };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditPassword(ResetPasswordViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageUsers))
            {
                return Forbid();
            }

            var user = await _userManager.FindByEmailAsync(model.Email) as User;

            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                if (await _userService.ResetPasswordAsync(model.Email, token, model.NewPassword, ModelState.AddModelError))
                {
                    _notifier.Success(H["Password updated correctly."]);

                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        private IActionResult RedirectToReturnUrlOrIndex(string returnUrl)
        {
            if ((String.IsNullOrEmpty(returnUrl) == false) && (Url.IsLocalUrl(returnUrl)))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
