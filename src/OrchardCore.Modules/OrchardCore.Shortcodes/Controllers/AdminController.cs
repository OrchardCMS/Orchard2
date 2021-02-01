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
using Newtonsoft.Json;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Liquid;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using OrchardCore.Shortcodes.Models;
using OrchardCore.Shortcodes.Services;
using OrchardCore.Shortcodes.ViewModels;

namespace OrchardCore.Shortcodes.Controllers
{
    [Feature("OrchardCore.Shortcodes.Templates")]
    public class AdminController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ShortcodeTemplatesManager _shortcodeTemplatesManager;
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly ISiteService _siteService;
        private readonly INotifier _notifier;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly dynamic New;

        public AdminController(
            IAuthorizationService authorizationService,
            ShortcodeTemplatesManager shortcodeTemplatesManager,
            ILiquidTemplateManager liquidTemplateManager,
            ISiteService siteService,
            INotifier notifier,
            IShapeFactory shapeFactory,
            IStringLocalizer<AdminController> stringLocalizer,
            IHtmlLocalizer<AdminController> htmlLocalizer
            )
        {
            _authorizationService = authorizationService;
            _shortcodeTemplatesManager = shortcodeTemplatesManager;
            _liquidTemplateManager = liquidTemplateManager;
            _siteService = siteService;
            _notifier = notifier;
            New = shapeFactory;
            S = stringLocalizer;
            H = htmlLocalizer;
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);
            var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.GetShortcodeTemplatesDocumentAsync();

            var shortcodeTemplates = shortcodeTemplatesDocument.ShortcodeTemplates.ToList();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                shortcodeTemplates = shortcodeTemplates.Where(x => x.Key.Contains(options.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var count = shortcodeTemplates.Count;

            shortcodeTemplates = shortcodeTemplates.OrderBy(x => x.Key)
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize).ToList();

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count);

            var model = new ShortcodeTemplateIndexViewModel
            {
                ShortcodeTemplates = shortcodeTemplates.Select(x => new ShortcodeTemplateEntry { Name = x.Key, ShortcodeTemplate = x.Value }).ToList(),
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
        public ActionResult IndexFilterPOST(ShortcodeTemplateIndexViewModel model)
        {
            return RedirectToAction("Index", new RouteValueDictionary {
                { "Options.Search", model.Options.Search }
            });
        }

        public async Task<IActionResult> Create(ShortcodeTemplateViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            return View(new ShortcodeTemplateViewModel());
        }

        [HttpPost, ActionName("Create")]
        public async Task<IActionResult> CreatePost(ShortcodeTemplateViewModel model, string submit, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Name), S["The name is mandatory."]);
                }
                else if (String.IsNullOrEmpty(model.Content))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Content), S["The template content is mandatory."]);
                }
                else if (!_liquidTemplateManager.Validate(model.Content, out var errors))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Content), S["The template doesn't contain a valid Liquid expression. Details: {0}", string.Join(" ", errors)]);
                }
                else
                {
                    var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.GetShortcodeTemplatesDocumentAsync();

                    if (shortcodeTemplatesDocument.ShortcodeTemplates.ContainsKey(model.Name))
                    {
                        ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Name), S["A template with the same name already exists."]);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var template = new ShortcodeTemplate
                {
                    Content = model.Content,
                    Hint = model.Hint,
                    Usage = model.Usage,
                    DefaultValue = model.DefaultValue,
                    Categories = JsonConvert.DeserializeObject<string[]>(model.SelectedCategories)
                };

                await _shortcodeTemplatesManager.UpdateShortcodeTemplateAsync(model.Name, template);

                if (submit == "SaveAndContinue")
                {
                    return RedirectToAction(nameof(Edit), new { name = model.Name });
                }
                else
                {
                    return RedirectToReturnUrlOrIndex(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> Edit(string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.GetShortcodeTemplatesDocumentAsync();

            if (!shortcodeTemplatesDocument.ShortcodeTemplates.ContainsKey(name))
            {
                return RedirectToAction("Create", new { name });
            }

            var template = shortcodeTemplatesDocument.ShortcodeTemplates[name];

            var model = new ShortcodeTemplateViewModel
            {
                Name = name,
                Content = template.Content,
                Hint = template.Hint,
                Usage = template.Usage,
                DefaultValue = template.DefaultValue,
                Categories = template.Categories
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string sourceName, ShortcodeTemplateViewModel model, string submit, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.LoadShortcodeTemplatesDocumentAsync();

            if (!shortcodeTemplatesDocument.ShortcodeTemplates.ContainsKey(sourceName))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Name), S["The name is mandatory."]);
                }
                else if (String.IsNullOrEmpty(model.Content))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Content), S["The template content is mandatory."]);
                }
                else if (!_liquidTemplateManager.Validate(model.Content, out var errors))
                {
                    ModelState.AddModelError(nameof(ShortcodeTemplateViewModel.Content), S["The template doesn't contain a valid Liquid expression. Details: {0}", string.Join(" ", errors)]);
                }
            }

            if (ModelState.IsValid)
            {
                var template = new ShortcodeTemplate
                {
                    Content = model.Content,
                    Hint = model.Hint,
                    Usage = model.Usage,
                    DefaultValue = model.DefaultValue,
                    Categories = JsonConvert.DeserializeObject<string[]>(model.SelectedCategories)
                };

                await _shortcodeTemplatesManager.RemoveShortcodeTemplateAsync(sourceName);

                await _shortcodeTemplatesManager.UpdateShortcodeTemplateAsync(model.Name, template);

                if (submit != "SaveAndContinue")
                {
                    return RedirectToReturnUrlOrIndex(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string name, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.LoadShortcodeTemplatesDocumentAsync();

            if (!shortcodeTemplatesDocument.ShortcodeTemplates.ContainsKey(name))
            {
                return NotFound();
            }

            await _shortcodeTemplatesManager.RemoveShortcodeTemplateAsync(name);

            _notifier.Success(H["Shortcode template deleted successfully."]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<IActionResult> IndexPost(ViewModels.ContentOptions options, IEnumerable<string> itemIds, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageShortcodeTemplates))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var shortcodeTemplatesDocument = await _shortcodeTemplatesManager.LoadShortcodeTemplatesDocumentAsync();
                var checkedContentItems = shortcodeTemplatesDocument.ShortcodeTemplates.Where(x => itemIds.Contains(x.Key));
                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in checkedContentItems)
                        {
                            await _shortcodeTemplatesManager.RemoveShortcodeTemplateAsync(item.Key);
                        }
                        _notifier.Success(H["Shortcode templates successfully removed."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToReturnUrlOrIndex(returnUrl);
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
