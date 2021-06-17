using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Entities;
using OrchardCore.Modules;
using OrchardCore.Scripting;
using OrchardCore.Settings;
using OrchardCore.Users.Events;
using OrchardCore.Users.Handlers;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Users.ViewModels;
using IWorkflowManager = OrchardCore.Workflows.Services.IWorkflowManager;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace OrchardCore.Users.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly SignInManager<IUser> _signInManager;
        private readonly UserManager<IUser> _userManager;
        private readonly ILogger _logger;
        private readonly ISiteService _siteService;
        private readonly IEnumerable<ILoginFormEvent> _accountEvents;
        private readonly IScriptingManager _scriptingManager;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly INotifier _notifier;
        private readonly IClock _clock;
        private readonly IDistributedCache _distributedCache;
        private readonly IEnumerable<IExternalLoginEventHandler> _externalLoginHandlers;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;

        public AccountController(
            IUserService userService,
            SignInManager<IUser> signInManager,
            UserManager<IUser> userManager,
            ILogger<AccountController> logger,
            ISiteService siteService,
            IHtmlLocalizer<AccountController> htmlLocalizer,
            IStringLocalizer<AccountController> stringLocalizer,
            IEnumerable<ILoginFormEvent> accountEvents,
            IScriptingManager scriptingManager,
            INotifier notifier,
            IClock clock,
            IDistributedCache distributedCache,
            IDataProtectionProvider dataProtectionProvider,
            IEnumerable<IExternalLoginEventHandler> externalLoginHandlers)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userService = userService;
            _logger = logger;
            _siteService = siteService;
            _accountEvents = accountEvents;
            _scriptingManager = scriptingManager;
            _notifier = notifier;
            _clock = clock;
            _distributedCache = distributedCache;
            _dataProtectionProvider = dataProtectionProvider;
            _externalLoginHandlers = externalLoginHandlers;
            H = htmlLocalizer;
            S = stringLocalizer;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (HttpContext.User != null && HttpContext.User.Identity.IsAuthenticated)
            {
                returnUrl = null;
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var loginSettings = (await _siteService.GetSiteSettingsAsync()).As<LoginSettings>();
            if (loginSettings.UseExternalProviderIfOnlyOneDefined)
            {
                var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
                if (schemes.Count() == 1)
                {
                    var dataProtector = _dataProtectionProvider.CreateProtector(nameof(DefaultExternalLogin))
                                            .ToTimeLimitedDataProtector();

                    var token = Guid.NewGuid();
                    var expiration = new TimeSpan(0, 0, 5);
                    var protectedToken = dataProtector.Protect(token.ToString(), _clock.UtcNow.Add(expiration));
                    await _distributedCache.SetAsync(token.ToString(), token.ToByteArray(), new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = expiration });
                    return RedirectToAction(nameof(DefaultExternalLogin), new { protectedToken, returnUrl });
                }
            }

            foreach (var errorMessage in TempData.Where(x => x.Key.StartsWith("error")).Select(x => x.Value.ToString()))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DefaultExternalLogin(string protectedToken, string returnUrl = null)
        {
            var loginSettings = (await _siteService.GetSiteSettingsAsync()).As<LoginSettings>();
            if (loginSettings.UseExternalProviderIfOnlyOneDefined)
            {
                var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
                if (schemes.Count() == 1)
                {
                    var dataProtector = _dataProtectionProvider.CreateProtector(nameof(DefaultExternalLogin))
                                            .ToTimeLimitedDataProtector();
                    try
                    {
                        Guid token;
                        if (Guid.TryParse(dataProtector.Unprotect(protectedToken), out token))
                        {
                            byte[] tokenBytes = await _distributedCache.GetAsync(token.ToString());
                            var cacheToken = new Guid(tokenBytes);
                            if (token.Equals(cacheToken))
                            {
                                return ExternalLogin(schemes.First().Name, returnUrl);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while validating DefaultExternalLogin token");
                    }
                }
            }
            return RedirectToAction(nameof(Login));
        }

        private async Task<bool> AddConfirmEmailError(IUser user)
        {
            var registrationSettings = (await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>();
            if (registrationSettings.UsersMustValidateEmail == true)
            {
                // Require that the users have a confirmed email before they can log on.
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, S["You must confirm your email."]);
                    return true;
                }
            }

            return false;
        }

        private bool AddUserEnabledError(IUser user)
        {
            var localUser = user as User;

            if (localUser == null || !localUser.IsEnabled)
            {
                ModelState.AddModelError(String.Empty, S["The specified user is not allowed to sign in."]);
                return true;
            }

            return false;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (TryValidateModel(model) && ModelState.IsValid)
            {
                var disableLocalLogin = (await _siteService.GetSiteSettingsAsync()).As<LoginSettings>().DisableLocalLogin;
                if (disableLocalLogin)
                {
                    ModelState.AddModelError("", S["Local login is disabled."]);
                }
                else
                {
                    await _accountEvents.InvokeAsync((e, model, modelState) => e.LoggingInAsync(model.UserName, (key, message) => modelState.AddModelError(key, message)), model, ModelState, _logger);
                    if (ModelState.IsValid)
                    {
                        var user = await _userManager.FindByNameAsync(model.UserName) ?? await _userManager.FindByEmailAsync(model.UserName);
                        // This doesn't count login failures towards account lockout
                        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                        if (user != null)
                        {
                            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
                            if (result.Succeeded)
                            {
                                if (!await AddConfirmEmailError(user) && !AddUserEnabledError(user))
                                {
                                    result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

                                    if (result.Succeeded)
                                    {
                                        _logger.LogInformation(1, "User logged in.");
                                        await _accountEvents.InvokeAsync((e, model) => e.LoggedInAsync(model.UserName), model, _logger);
                                        return await LoggedInActionResult(user, returnUrl);
                                    }
                                }
                            }

                            if (result.IsLockedOut)
                            {
                                ModelState.AddModelError(string.Empty, S["The account is locked out"]);
                                return View();
                            }
                        }

                        ModelState.AddModelError(string.Empty, S["Invalid login attempt."]);
                    }

                    await _accountEvents.InvokeAsync((e, model) => e.LoggingInFailedAsync(model.UserName), model, _logger);
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");

            return Redirect("~/");
        }

        [HttpGet]
        public IActionResult ChangePassword(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, string returnUrl = null)
        {
            if (TryValidateModel(model) && ModelState.IsValid)
            {
                var user = await _userService.GetAuthenticatedUserAsync(User);
                if (await _userService.ChangePasswordAsync(user, model.CurrentPassword, model.Password, (key, message) => ModelState.AddModelError(key, message)))
                {
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        _notifier.Success(H["Your password has been changed successfully."]);
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return Redirect(Url.Action("ChangePasswordConfirmation"));
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePasswordConfirmation()
        {
            return View();
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("~/");
            }
        }

        private async Task<IActionResult> LoggedInActionResult(IUser user, string returnUrl = null, ExternalLoginInfo info = null)
        {
            var workflowManager = HttpContext.RequestServices.GetService<IWorkflowManager>();
            if (workflowManager != null)
            {
                var input = new Dictionary<string, object>();
                input["UserName"] = user.UserName;
                input["ExternalClaims"] = info == null ? Enumerable.Empty<SerializableClaim>() : info.Principal.GetSerializableClaims();
                input["Roles"] = ((User)user).RoleNames;
                input["Provider"] = info?.LoginProvider;
                await workflowManager.TriggerEventAsync(nameof(Workflows.Activities.UserLoggedInEvent),
                    input: input, correlationId: ((User)user).UserId);
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        private async Task<SignInResult> ExternalLoginSignInAsync(IUser user, ExternalLoginInfo info)
        {
            var claims = info.Principal.GetSerializableClaims();
            var userRoles = await _userManager.GetRolesAsync(user);
            var context = new UpdateRolesContext(user, info.LoginProvider, claims, userRoles);

            string[] rolesToAdd = new string[0];
            string[] rolesToRemove = new string[0];

            var loginSettings = (await _siteService.GetSiteSettingsAsync()).As<LoginSettings>();
            if (loginSettings.UseScriptToSyncRoles)
            {
                try
                {
                    var jsonSerializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    var script = $"js: function syncRoles(context) {{\n{loginSettings.SyncRolesScript}\n}}\nvar context={JsonConvert.SerializeObject(context, jsonSerializerSettings)};\nsyncRoles(context);\nreturn context;";
                    dynamic evaluationResult = await _scriptingManager.EvaluateAsync(script, null, null, null);
                    rolesToAdd = (evaluationResult.rolesToAdd as object[]).Select(i => i.ToString()).ToArray();
                    rolesToRemove = (evaluationResult.rolesToRemove as object[]).Select(i => i.ToString()).ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error Syncing Roles From External Provider {0}", info.LoginProvider);
                }
            }
            else
            {
                foreach (var item in _externalLoginHandlers)
                {
                    try
                    {
                        await item.UpdateRoles(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{externalLoginHandler} - IExternalLoginHandler.UpdateRoles threw an exception", item.GetType());
                    }
                }
                rolesToAdd = context.RolesToAdd;
                rolesToRemove = context.RolesToRemove;
            }

            await _userManager.AddToRolesAsync(user, rolesToAdd.Distinct());
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove.Distinct());

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                var identityResult = await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                if (!identityResult.Succeeded)
                {
                    _logger.LogError("Error updating the external authentication tokens.");
                }
            }

            return result;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                _logger.LogError("Error from external provider: {Error}", remoteError);
                ModelState.AddModelError("", S["An error occurred in external provider."]);
                return RedirectToLogin(returnUrl);
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Could not get external login info.");
                ModelState.AddModelError("", S["An error occurred in external provider."]);
                return RedirectToLogin(returnUrl);
            }

            var registrationSettings = (await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>();
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user != null)
            {
                if (!await AddConfirmEmailError(user) && !AddUserEnabledError(user))
                {
                    await _accountEvents.InvokeAsync((e, user, modelState) => e.LoggingInAsync(user.UserName, (key, message) => modelState.AddModelError(key, message)), user, ModelState, _logger);

                    var signInResult = await ExternalLoginSignInAsync(user, info);
                    if (signInResult.Succeeded)
                    {
                        return await LoggedInActionResult(user, returnUrl, info);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, S["Invalid login attempt."]);
                    }
                }
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");

                if (!string.IsNullOrWhiteSpace(email))
                    user = await _userManager.FindByEmailAsync(email);

                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;

                if (user != null)
                {
                    // Link external login to an existing user
                    ViewData["UserName"] = user.UserName;
                    ViewData["Email"] = email;

                    return View(nameof(LinkExternalLogin));
                }
                else
                {
                    // no user could be matched, check if a new user can register
                    if (registrationSettings.UsersCanRegister == UserRegistrationType.NoRegistration)
                    {
                        string message = S["Site does not allow user registration."];
                        _logger.LogWarning(message);
                        ModelState.AddModelError("", message);
                    }
                    else
                    {
                        var externalLoginViewModel = new RegisterExternalLoginViewModel();

                        externalLoginViewModel.NoPassword = registrationSettings.NoPasswordForExternalUsers;
                        externalLoginViewModel.NoEmail = registrationSettings.NoEmailForExternalUsers;
                        externalLoginViewModel.NoUsername = registrationSettings.NoUsernameForExternalUsers;

                        // If registrationSettings.NoUsernameForExternalUsers is true, this username will not be used
                        externalLoginViewModel.UserName = await GenerateUsername(info);
                        externalLoginViewModel.Email = email;

                        // The user doesn't exist, if no information required, we can create the account locally
                        // instead of redirecting to the ExternalLogin
                        var noInformationRequired = externalLoginViewModel.NoPassword
                                                        && externalLoginViewModel.NoEmail
                                                        && externalLoginViewModel.NoUsername;

                        if (noInformationRequired)
                        {
                            user = await this.RegisterUser(new RegisterViewModel()
                            {
                                UserName = externalLoginViewModel.UserName,
                                Email = externalLoginViewModel.Email,
                                Password = null,
                                ConfirmPassword = null
                            }, S["Confirm your account"], _logger);

                            // If the registration was successful we can link the external provider and redirect the user
                            if (user != null)
                            {
                                var identityResult = await _signInManager.UserManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.ProviderDisplayName));
                                if (identityResult.Succeeded)
                                {
                                    _logger.LogInformation(3, "User account linked to {LoginProvider} provider.", info.LoginProvider);

                                    // We have created/linked to the local user, so we must verify the login.
                                    // If it does not succeed, the user is not allowed to login
                                    var signInResult = await ExternalLoginSignInAsync(user, info);
                                    if (signInResult.Succeeded)
                                    {
                                        return await LoggedInActionResult(user, returnUrl, info);
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, S["Invalid login attempt."]);
                                        return RedirectToLogin(returnUrl);
                                    }
                                }
                                AddIdentityErrors(identityResult);
                            }
                        }
                        return View("RegisterExternalLogin", externalLoginViewModel);
                    }
                }
            }
            return RedirectToLogin(returnUrl);
        }

        private RedirectToActionResult RedirectToLogin(string returnUrl)
        {
            var iix = 0;
            foreach (var state in ModelState.Where(x => x.Key == string.Empty))
            {
                foreach (var item in state.Value.Errors)
                {
                    TempData[$"error_{iix++}"] = item.ErrorMessage;
                }
            }
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterExternalLogin(RegisterExternalLoginViewModel model, string returnUrl = null)
        {
            IUser user = null;
            var settings = (await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>();
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                _logger.LogWarning("Error loading external login info.");
                return NotFound();
            }

            if (settings.UsersCanRegister == UserRegistrationType.NoRegistration)
            {
                _logger.LogWarning("Site does not allow user registration.", model.UserName, model.Email);
                return NotFound();
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;

            model.NoPassword = settings.NoPasswordForExternalUsers;
            model.NoEmail = settings.NoEmailForExternalUsers;
            model.NoUsername = settings.NoUsernameForExternalUsers;

            ModelState.Clear();

            if (model.NoEmail)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");
                model.Email = email;
            }

            if (model.NoUsername)
            {
                model.UserName = await GenerateUsername(info);
            }

            if (model.NoPassword)
            {
                model.Password = null;
                model.ConfirmPassword = null;
            }

            if (TryValidateModel(model) && ModelState.IsValid)
            {
                user = await this.RegisterUser(new RegisterViewModel() { UserName = model.UserName, Email = model.Email, Password = model.Password, ConfirmPassword = model.ConfirmPassword }, S["Confirm your account"], _logger);
                if (user is null)
                {
                    ModelState.AddModelError(string.Empty, "Registration Failed.");
                }
                else
                {
                    var identityResult = await _signInManager.UserManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.ProviderDisplayName));
                    if (identityResult.Succeeded)
                    {
                        _logger.LogInformation(3, "User account linked to {provider} provider.", info.LoginProvider);

                        // we have created/linked to the local user, so we must verify the login. If it does not succeed,
                        // the user is not allowed to login
                        var signInResult = await ExternalLoginSignInAsync(user, info);
                        if (signInResult.Succeeded)
                        {
                            return await LoggedInActionResult(user, returnUrl, info);
                        }
                    }
                    AddIdentityErrors(identityResult);
                }
            }
            return View("RegisterExternalLogin", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkExternalLogin(LinkExternalLoginViewModel model, string returnUrl = null)
        {
            var settings = (await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>();
            var info = await _signInManager.GetExternalLoginInfoAsync();

            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue("email");

            var user = await _userManager.FindByEmailAsync(email);

            if (info == null)
            {
                _logger.LogWarning("Error loading external login info.");
                return NotFound();
            }

            if (user == null)
            {
                _logger.LogWarning("Suspicious login detected from external provider. {provider} with key [{providerKey}] for {identity}",
                    info.LoginProvider, info.ProviderKey, info.Principal?.Identity?.Name);
                return RedirectToAction(nameof(Login));
            }

            await _accountEvents.InvokeAsync((e, model, modelState) => e.LoggingInAsync(user.UserName, (key, message) => modelState.AddModelError(key, message)), model, ModelState, _logger);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!signInResult.Succeeded)
            {
                user = null;
                ModelState.AddModelError(string.Empty, S["Invalid login attempt."]);
            }
            else
            {
                var identityResult = await _signInManager.UserManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.ProviderDisplayName));
                if (identityResult.Succeeded)
                {
                    _logger.LogInformation(3, "User account linked to {provider} provider.", info.LoginProvider);
                    // we have created/linked to the local user, so we must verify the login. If it does not succeed,
                    // the user is not allowed to login
                    if ((await ExternalLoginSignInAsync(user, info)).Succeeded)
                    {
                        return await LoggedInActionResult(user, returnUrl, info);
                    }
                }
                AddIdentityErrors(identityResult);
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var model = new ExternalLoginsViewModel { CurrentLogins = await _userManager.GetLoginsAsync(user) };
            model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => model.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            model.ShowRemoveButton = await _userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
            //model.StatusMessage = StatusMessage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("Unable to load user with ID '{UserId}'.", _userManager.GetUserId(User));
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Unexpected error occurred loading external login info for user '{UserName}'.", user.UserName);
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.ProviderDisplayName));
            if (!result.Succeeded)
            {
                _logger.LogError("Unexpected error occurred adding external login info for user '{UserName}'.", user.UserName);
                return RedirectToAction(nameof(Login));
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            // Perform External Login SignIn
            await ExternalLoginSignInAsync(user, info);
            //StatusMessage = "The external login was added.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("Unable to load user with ID '{UserId}'.", _userManager.GetUserId(User));
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded)
            {
                _logger.LogError("Unexpected error occurred removing external login info for user '{UserName}'.", user.UserName);
                return RedirectToAction(nameof(Login));
            }

            // Remove External Authentication Tokens
            foreach (var item in ((User)user).UserTokens.Where(c => c.LoginProvider == model.LoginProvider).ToList())
            {
                if (!(await (_userManager.RemoveAuthenticationTokenAsync(user, model.LoginProvider, item.Name))).Succeeded)
                {
                    _logger.LogError("Could not remove '{TokenName}' token while unlinking '{LoginProvider}' provider from user '{UserName}'.", item.Name, model.LoginProvider, user.UserName);
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            //StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        private async Task<string> GenerateUsername(ExternalLoginInfo info)
        {
            var now = new TimeSpan(_clock.UtcNow.Ticks) - new TimeSpan(DateTime.UnixEpoch.Ticks);
            var ret = string.Concat("u" + Convert.ToInt32(now.TotalSeconds).ToString());

            var registrationSettings = (await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>();

            var externalClaims = info == null ? null : info.Principal.GetSerializableClaims();

            if (registrationSettings.UseScriptToGenerateUsername)
            {
                var context = new { userName = string.Empty, loginProvider = info?.LoginProvider, externalClaims };
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                var script = $"js: function generateUsername(context) {{\n{registrationSettings.GenerateUsernameScript}\n}}\nvar context = {JsonConvert.SerializeObject(context, jsonSerializerSettings)};\ngenerateUsername(context);\nreturn context;";
                try
                {
                    dynamic evaluationResult = await _scriptingManager.EvaluateAsync(script, null, null, null);
                    if (evaluationResult?.userName == null)
                        throw new Exception("GenerateUsernameScript did not return a username");
                    return evaluationResult.userName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating GenerateUsernameScript({context})", context);
                }
            }
            else
            {
                var userNames = new Dictionary<Type, string>();
                foreach (var item in _externalLoginHandlers)
                {
                    try
                    {
                        var userName = await item.GenerateUserName(info.LoginProvider, externalClaims.ToArray());
                        if (!string.IsNullOrWhiteSpace(userName))
                        {
                            if (userNames.Count == 0)
                            {
                                ret = userName;
                            }
                            userNames.Add(item.GetType(), userName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{externalLoginHandler} - IExternalLoginHandler.GenerateUserName threw an exception", item.GetType());
                    }
                }
                if (userNames.Count > 1)
                {
                    _logger.LogWarning("More than one IExternalLoginHandler generated username. Used first one registered, {externalLoginHandler}", userNames.FirstOrDefault().Key);
                }
            }

            return ret;
        }
    }
}
