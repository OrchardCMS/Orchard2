using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.Deployment.Remote.Services;
using OrchardCore.Deployment.Remote.ViewModels;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;

namespace OrchardCore.Deployment.Remote.Controllers
{
    [Admin]
    public class RemoteInstanceController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ISiteService _siteService;
        private readonly INotifier _notifier;
        private readonly RemoteInstanceService _service;
        private readonly dynamic New;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;

        public RemoteInstanceController(
            RemoteInstanceService service,
            IAuthorizationService authorizationService,
            ISiteService siteService,
            IShapeFactory shapeFactory,
            IStringLocalizer<RemoteInstanceController> stringLocalizer,
            IHtmlLocalizer<RemoteInstanceController> htmlLocalizer,
            INotifier notifier
            )
        {
            _authorizationService = authorizationService;
            _siteService = siteService;
            New = shapeFactory;
            S = stringLocalizer;
            H = htmlLocalizer;
            _notifier = notifier;
            _service = service;
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);

            var remoteInstances = (await _service.GetRemoteInstanceListAsync()).RemoteInstances;

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                remoteInstances = remoteInstances.Where(x => x.Name.Contains(options.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var count = remoteInstances.Count();

            var startIndex = pager.GetStartIndex();
            var pageSize = pager.PageSize;

            // Maintain previous route data when generating page links
            var routeData = new RouteData();
            routeData.Values.Add("Options.Search", options.Search);

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count).RouteData(routeData);

            var model = new RemoteInstanceIndexViewModel
            {
                RemoteInstances = remoteInstances,
                Pager = pagerShape,
                Options = options
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Filter")]
        public ActionResult IndexFilterPOST(RemoteInstanceIndexViewModel model)
        {
            return RedirectToAction("Index", new RouteValueDictionary {
                { "Options.Search", model.Options.Search }
            });
        }

        public async Task<IActionResult> Create()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            var model = new EditRemoteInstanceViewModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EditRemoteInstanceViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                ValidateViewModel(model);
            }

            if (ModelState.IsValid)
            {
                await _service.CreateRemoteInstanceAsync(model.Name, model.Url, model.ClientName, model.ApiKey, model.ApiKeySecret, model.RsaEncryptionSecret, model.RsaSigningSecret);

                _notifier.Success(H["Remote instance created successfully."]);
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            var remoteInstance = await _service.GetRemoteInstanceAsync(id);

            if (remoteInstance == null)
            {
                return NotFound();
            }

            var model = new EditRemoteInstanceViewModel
            {
                Id = remoteInstance.Id,
                Name = remoteInstance.Name,
                ClientName = remoteInstance.ClientName,
                ApiKey = remoteInstance.ApiKey,
                ApiKeySecret = remoteInstance.ApiKeySecret,
                Url = remoteInstance.Url,
                RsaEncryptionSecret = remoteInstance.RsaEncryptionSecret,
                RsaSigningSecret = remoteInstance.RsaSigningSecret
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditRemoteInstanceViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            var remoteInstance = await _service.LoadRemoteInstanceAsync(model.Id);

            if (remoteInstance == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                ValidateViewModel(model);
            }

            if (ModelState.IsValid)
            {
                await _service.UpdateRemoteInstance(model.Id, model.Name, model.Url, model.ClientName, model.ApiKey, model.ApiKeySecret, model.RsaEncryptionSecret, model.RsaSigningSecret);

                _notifier.Success(H["Remote instance updated successfully."]);

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            var remoteInstance = await _service.LoadRemoteInstanceAsync(id);

            if (remoteInstance == null)
            {
                return NotFound();
            }

            await _service.DeleteRemoteInstanceAsync(id);

            _notifier.Success(H["Remote instance deleted successfully."]);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> IndexPost(ViewModels.ContentOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageRemoteInstances))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var remoteInstances = (await _service.LoadRemoteInstanceListAsync()).RemoteInstances;
                var checkedContentItems = remoteInstances.Where(x => itemIds.Contains(x.Id)).ToList();

                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in checkedContentItems)
                        {
                            await _service.DeleteRemoteInstanceAsync(item.Id);
                        }
                        _notifier.Success(H["Remote instances successfully removed."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction("Index");
        }

        private void ValidateViewModel(EditRemoteInstanceViewModel model)
        {
            if (String.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(EditRemoteInstanceViewModel.Name), S["The name is mandatory."]);
            }

            if (String.IsNullOrWhiteSpace(model.ClientName))
            {
                ModelState.AddModelError(nameof(EditRemoteInstanceViewModel.ClientName), S["The client name is mandatory."]);
            }

            if (String.IsNullOrWhiteSpace(model.ApiKey) && String.IsNullOrWhiteSpace(model.ApiKeySecret))
            {
                ModelState.AddModelError(nameof(EditRemoteInstanceViewModel.ApiKey), S["The api key is mandatory."]);
            }

            if (String.IsNullOrWhiteSpace(model.Url))
            {
                ModelState.AddModelError(nameof(EditRemoteInstanceViewModel.Url), S["The url is mandatory."]);
            }
            else
            {
                Uri uri;
                if (!Uri.TryCreate(model.Url, UriKind.Absolute, out uri))
                {
                    ModelState.AddModelError(nameof(EditRemoteInstanceViewModel.Url), S["The url is invalid."]);
                }
            }
        }
    }
}
