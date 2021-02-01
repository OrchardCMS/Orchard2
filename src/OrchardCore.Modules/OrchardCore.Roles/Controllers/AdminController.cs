using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Data.Documents;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Extensions;
using OrchardCore.Roles.ViewModels;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Security.Services;

namespace OrchardCore.Roles.Controllers
{
    public class AdminController : Controller
    {
        private readonly IDocumentStore _documentStore;
        private readonly IAuthorizationService _authorizationService;
        private readonly IStringLocalizer S;
        private readonly RoleManager<IRole> _roleManager;
        private readonly IEnumerable<IPermissionProvider> _permissionProviders;
        private readonly ITypeFeatureProvider _typeFeatureProvider;
        private readonly IRoleService _roleService;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer H;

        public AdminController(
            IAuthorizationService authorizationService,
            ITypeFeatureProvider typeFeatureProvider,
            IDocumentStore documentStore,
            IStringLocalizer<AdminController> stringLocalizer,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            RoleManager<IRole> roleManager,
            IRoleService roleService,
            INotifier notifier,
            IEnumerable<IPermissionProvider> permissionProviders
            )
        {
            H = htmlLocalizer;
            _notifier = notifier;
            _roleService = roleService;
            _typeFeatureProvider = typeFeatureProvider;
            _permissionProviders = permissionProviders;
            _roleManager = roleManager;
            S = stringLocalizer;
            _authorizationService = authorizationService;
            _documentStore = documentStore;
        }

        public async Task<ActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            {
                return Forbid();
            }

            var roles = await _roleService.GetRolesAsync();

            var model = new RolesViewModel
            {
                RoleEntries = roles.Select(BuildRoleEntry).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            {
                return Forbid();
            }

            var model = new CreateRoleViewModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRoleViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                model.RoleName = model.RoleName.Trim();

                if (model.RoleName.Contains('/'))
                {
                    ModelState.AddModelError(string.Empty, S["Invalid role name."]);
                }

                if (await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(model.RoleName)) != null)
                {
                    ModelState.AddModelError(string.Empty, S["The role is already used."]);
                }
            }

            if (ModelState.IsValid)
            {
                var role = new Role { RoleName = model.RoleName, RoleDescription = model.RoleDescription };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _notifier.Success(H["Role created successfully."]);
                    return RedirectToReturnUrlOrIndex(returnUrl);
                }

                _documentStore.Cancel();

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            {
                return Forbid();
            }

            var currentRole = await _roleManager.FindByIdAsync(id);

            if (currentRole == null)
            {
                return NotFound();
            }

            var result = await _roleManager.DeleteAsync(currentRole);

            if (result.Succeeded)
            {
                _notifier.Success(H["Role deleted successfully."]);
            }
            else
            {
                _documentStore.Cancel();

                _notifier.Error(H["Could not delete this role."]);

                foreach (var error in result.Errors)
                {
                    _notifier.Error(H[error.Description]);
                }
            }

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            {
                return Forbid();
            }

            var role = (Role)await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(id));
            if (role == null)
            {
                return NotFound();
            }

            var installedPermissions = await GetInstalledPermissionsAsync();
            var allPermissions = installedPermissions.SelectMany(x => x.Value);

            var model = new EditRoleViewModel
            {
                Role = role,
                Name = role.RoleName,
                RoleDescription = role.RoleDescription,
                EffectivePermissions = await GetEffectivePermissions(role, allPermissions),
                RoleCategoryPermissions = installedPermissions
            };

            return View(model);
        }

        [HttpPost, ActionName(nameof(Edit))]
        public async Task<IActionResult> EditPost(string id, string roleDescription, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRoles))
            {
                return Forbid();
            }

            var role = (Role)await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(id));

            if (role == null)
            {
                return NotFound();
            }

            role.RoleDescription = roleDescription;

            // Save
            var rolePermissions = new List<RoleClaim>();
            foreach (string key in Request.Form.Keys)
            {
                if (key.StartsWith("Checkbox.", StringComparison.Ordinal) && Request.Form[key] == "true")
                {
                    string permissionName = key.Substring("Checkbox.".Length);
                    rolePermissions.Add(new RoleClaim { ClaimType = Permission.ClaimType, ClaimValue = permissionName });
                }
            }

            role.RoleClaims.RemoveAll(c => c.ClaimType == Permission.ClaimType);
            role.RoleClaims.AddRange(rolePermissions);

            await _roleManager.UpdateAsync(role);

            _notifier.Success(H["Role updated successfully."]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        private RoleEntry BuildRoleEntry(IRole role)
        {
            return new RoleEntry
            {
                Name = role.RoleName,
                Description = role.RoleDescription,
                Selected = false
            };
        }

        private async Task<IDictionary<string, IEnumerable<Permission>>> GetInstalledPermissionsAsync()
        {
            var installedPermissions = new Dictionary<string, IEnumerable<Permission>>();
            foreach (var permissionProvider in _permissionProviders)
            {
                var feature = _typeFeatureProvider.GetFeatureForDependency(permissionProvider.GetType());
                var featureName = feature.Id;

                var permissions = await permissionProvider.GetPermissionsAsync();

                foreach (var permission in permissions)
                {
                    var category = permission.Category;

                    string title = String.IsNullOrWhiteSpace(category) ? S["{0} Feature", featureName] : category;

                    if (installedPermissions.ContainsKey(title))
                    {
                        installedPermissions[title] = installedPermissions[title].Concat(new[] { permission });
                    }
                    else
                    {
                        installedPermissions.Add(title, new[] { permission });
                    }
                }
            }

            return installedPermissions;
        }

        private async Task<IEnumerable<string>> GetEffectivePermissions(Role role, IEnumerable<Permission> allPermissions)
        {
            // Create a fake user to check the actual permissions. If the role is anonymous
            // IsAuthenticated needs to be false.
            var fakeUser = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role.RoleName) },
                role.RoleName != "Anonymous" ? "FakeAuthenticationType" : null)
            );

            var result = new List<string>();

            foreach (var permission in allPermissions)
            {
                if (await _authorizationService.AuthorizeAsync(fakeUser, permission))
                {
                    result.Add(permission.Name);
                }
            }

            return result;
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
