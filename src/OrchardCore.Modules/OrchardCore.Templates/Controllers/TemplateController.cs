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
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using OrchardCore.Templates.Models;
using OrchardCore.Templates.Services;
using OrchardCore.Templates.ViewModels;

namespace OrchardCore.Templates.Controllers
{
    [Admin]
    public class TemplateController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly TemplatesManager _templatesManager;
        private readonly AdminTemplatesManager _adminTemplatesManager;
        private readonly ISiteService _siteService;
        private readonly INotifier _notifier;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly dynamic New;

        public TemplateController(
            IAuthorizationService authorizationService,
            TemplatesManager templatesManager,
            AdminTemplatesManager adminTemplatesManager,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            IStringLocalizer<TemplateController> stringLocalizer,
            IHtmlLocalizer<TemplateController> htmlLocalizer,
            INotifier notifier)
        {
            _authorizationService = authorizationService;
            _templatesManager = templatesManager;
            _adminTemplatesManager = adminTemplatesManager;
            New = shapeFactory;
            _siteService = siteService;
            _notifier = notifier;
            S = stringLocalizer;
            H = htmlLocalizer;
        }

        public Task<IActionResult> Admin(ContentOptions options, PagerParameters pagerParameters)
        {
            options.AdminTemplates = true;

            // Used to provide a different url such that the Admin Templates menu entry doesn't collide with the Templates ones
            return Index(options, pagerParameters);
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!options.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (options.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);
            var templatesDocument = options.AdminTemplates
                ? await _adminTemplatesManager.GetTemplatesDocumentAsync()
                : await _templatesManager.GetTemplatesDocumentAsync()
                ;

            var templates = templatesDocument.Templates.ToList();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                templates = templates.Where(x => x.Key.Contains(options.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var count = templates.Count;

            templates = templates.OrderBy(x => x.Key)
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize).ToList();

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count);

            var model = new TemplateIndexViewModel
            {
                Templates = templates.Select(x => new TemplateEntry { Name = x.Key, Template = x.Value }).ToList(),
                Options = options,
                Pager = pagerShape
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            return View("Index", model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Filter")]
        public ActionResult IndexFilterPOST(TemplateIndexViewModel model)
        {
            return RedirectToAction(nameof(Index), new RouteValueDictionary {
                { "Options.Search", model.Options.Search }
            });
        }

        public async Task<IActionResult> Create(TemplateViewModel model, bool adminTemplates = false, string returnUrl = null)
        {
            if (!adminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (adminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new TemplateViewModel() { AdminTemplates = adminTemplates });
        }

        [HttpPost, ActionName("Create")]
        public async Task<IActionResult> CreatePost(TemplateViewModel model, string submit, string returnUrl = null)
        {
            if (!model.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (model.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(TemplateViewModel.Name), S["The name is mandatory."]);
                }
                else if (String.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError(nameof(TemplateViewModel.Content), S["The content is mandatory."]);
                }
                else
                {
                    var templatesDocument = model.AdminTemplates
                        ? await _adminTemplatesManager.GetTemplatesDocumentAsync()
                        : await _templatesManager.GetTemplatesDocumentAsync()
                        ;

                    if (templatesDocument.Templates.ContainsKey(model.Name))
                    {
                        ModelState.AddModelError(nameof(TemplateViewModel.Name), S["A template with the same name already exists."]);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var template = new Template { Content = model.Content, Description = model.Description };

                await (model.AdminTemplates
                    ? _adminTemplatesManager.UpdateTemplateAsync(model.Name, template)
                    : _templatesManager.UpdateTemplateAsync(model.Name, template)
                    );

                _notifier.Success(H["The \"{0}\" template has been created.", model.Name]);

                if (submit == "SaveAndContinue")
                {
                    return RedirectToAction(nameof(Edit), new { name = model.Name, adminTemplates = model.AdminTemplates, returnUrl });
                }
                else
                {
                    return RedirectToReturnUrlOrIndex(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> Edit(string name, bool adminTemplates = false, string returnUrl = null)
        {
            if (!adminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (adminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            var templatesDocument = adminTemplates
                ? await _adminTemplatesManager.GetTemplatesDocumentAsync()
                : await _templatesManager.GetTemplatesDocumentAsync()
                ;

            if (!templatesDocument.Templates.ContainsKey(name))
            {                
                return RedirectToAction(nameof(Create), new { name, returnUrl, adminTemplates });
            }

            var template = templatesDocument.Templates[name];

            var model = new TemplateViewModel
            {
                AdminTemplates = adminTemplates,
                Name = name,
                Content = template.Content,
                Description = template.Description
            };

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string sourceName, TemplateViewModel model, string submit, string returnUrl = null)
        {
            if (!model.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (model.AdminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            var templatesDocument = model.AdminTemplates
                ? await _adminTemplatesManager.LoadTemplatesDocumentAsync()
                : await _templatesManager.LoadTemplatesDocumentAsync()
                ;

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(TemplateViewModel.Name), S["The name is mandatory."]);
                }
                else if (!model.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase) && templatesDocument.Templates.ContainsKey(model.Name))
                {
                    ModelState.AddModelError(nameof(TemplateViewModel.Name), S["A template with the same name already exists."]);
                }
                else if (String.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError(nameof(TemplateViewModel.Content), S["The content is mandatory."]);
                }
            }

            if (!templatesDocument.Templates.ContainsKey(sourceName))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var template = new Template { Content = model.Content, Description = model.Description };

                await (model.AdminTemplates
                    ? _adminTemplatesManager.RemoveTemplateAsync(sourceName)
                    : _templatesManager.RemoveTemplateAsync(sourceName)
                    );

                await (model.AdminTemplates
                    ? _adminTemplatesManager.UpdateTemplateAsync(model.Name, template)
                    : _templatesManager.UpdateTemplateAsync(model.Name, template)
                    );

                if (submit != "SaveAndContinue")
                {
                    return RedirectToReturnUrlOrIndex(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string name, string returnUrl, bool adminTemplates = false)
        {
            if (!adminTemplates && !await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (adminTemplates && !await _authorizationService.AuthorizeAsync(User, AdminTemplatesPermissions.ManageAdminTemplates))
            {
                return Forbid();
            }

            var templatesDocument = adminTemplates
                ? await _adminTemplatesManager.LoadTemplatesDocumentAsync()
                : await _templatesManager.LoadTemplatesDocumentAsync();

            if (!templatesDocument.Templates.ContainsKey(name))
            {
                return NotFound();
            }

            await (adminTemplates
                    ? _adminTemplatesManager.RemoveTemplateAsync(name)
                    : _templatesManager.RemoveTemplateAsync(name));

            _notifier.Success(H["Template deleted successfully."]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> ListPost(ContentOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTemplates))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var templatesDocument = options.AdminTemplates
                        ? await _adminTemplatesManager.LoadTemplatesDocumentAsync()
                        : await _templatesManager.LoadTemplatesDocumentAsync();
                var checkedContentItems = templatesDocument.Templates.Where(x => itemIds.Contains(x.Key));

                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in checkedContentItems)
                        {
                            await (options.AdminTemplates
                                    ? _adminTemplatesManager.RemoveTemplateAsync(item.Key)
                                    : _templatesManager.RemoveTemplateAsync(item.Key));
                        }
                        _notifier.Success(H["Templates successfully removed."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (options.AdminTemplates)
            {
                return RedirectToAction(nameof(Admin));
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
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
