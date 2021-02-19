using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using OrchardCore.Secrets.ViewModels;

namespace OrchardCore.Secrets.Controllers
{
    [Admin]
    public class AdminController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ISecretCoordinator _secretCoordinator;
        private readonly IDisplayManager<Secret> _displayManager;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IEnumerable<ISecretFactory> _factories;
        private readonly ISiteService _siteService;
        private readonly INotifier _notifier;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly dynamic New;

        public AdminController(
            IAuthorizationService authorizationService,
            ISecretCoordinator secretCoordinator,
            IDisplayManager<Secret> displayManager,
            IUpdateModelAccessor updateModelAccessor,
            IEnumerable<ISecretFactory> factories,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            IStringLocalizer<AdminController> stringLocalizer,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            INotifier notifier)
        {
            _authorizationService = authorizationService;
            _secretCoordinator = secretCoordinator;
            _displayManager = displayManager;
            _updateModelAccessor = updateModelAccessor;
            _factories = factories;
            New = shapeFactory;
            _siteService = siteService;
            _notifier = notifier;
            S = stringLocalizer;
            H = htmlLocalizer;
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);
            var secretBindings = (await _secretCoordinator.GetSecretBindingsAsync()).ToList();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                secretBindings = secretBindings.Where(x => x.Key.Contains(options.Search)).ToList();
            }

            var count = secretBindings.Count;

            secretBindings = secretBindings.OrderBy(x => x.Key)
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize).ToList();

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count);

            var thumbnails = new Dictionary<string, dynamic>();
            foreach (var factory in _factories)
            {
                var secret = factory.Create();
                dynamic thumbnail = await _displayManager.BuildDisplayAsync(secret, _updateModelAccessor.ModelUpdater, "Thumbnail");
                thumbnail.Secret = secret;
                thumbnails.Add(factory.Name, thumbnail);
            }

            var bindingEntries = new List<SecretBindingEntry>();
            foreach (var binding in secretBindings)
            {
                var secret = _factories.FirstOrDefault(x => x.Name == binding.Value.Type)?.Create();
                secret = await _secretCoordinator.GetSecretAsync(binding.Key, secret.GetType());
                if (secret == null)
                {
                    continue;
                }
                dynamic summary = await _displayManager.BuildDisplayAsync(secret, _updateModelAccessor.ModelUpdater, "Summary");
                summary.Secret = secret;
                bindingEntries.Add(new SecretBindingEntry
                {
                    Name = binding.Key,
                    SecretBinding = binding.Value,
                    Summary = summary
                });
            };

            var model = new SecretBindingIndexViewModel
            {
                SecretBindings = bindingEntries,
                Thumbnails = thumbnails,
                Options = options,
                Pager = pagerShape
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            return View("Index", model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> IndexPost(ContentOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var secretBindings = await _secretCoordinator.GetSecretBindingsAsync();
                var checkedSecretBindings = secretBindings.Where(x => itemIds.Contains(x.Key));
                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var binding in checkedSecretBindings)
                        {
                            await _secretCoordinator.RemoveSecretAsync(binding.Key, binding.Value.Store);
                        }
                        _notifier.Success(H["Secrets successfully removed."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Create(string type)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var secret = _factories.FirstOrDefault(x => x.Name == type)?.Create();

            if (secret == null)
            {
                return NotFound();
            }

            secret.Id = Guid.NewGuid().ToString("n");

            var model = new SecretBindingViewModel
            {
                SecretId = secret.Id,
                Secret = secret,
                Type = type,
                StoreEntries = _secretCoordinator.ToArray(),
                Editor = await _displayManager.BuildEditorAsync(secret, updater: _updateModelAccessor.ModelUpdater, isNew: true)
            };

            model.Editor.Secret = secret;

            return View(model);
        }

        [HttpPost, ActionName("Create")]
        public async Task<IActionResult> CreatePost(SecretBindingViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var secret = _factories.FirstOrDefault(x => x.Name == model.Type)?.Create();

            if (secret == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["The name is mandatory."]);
                }
                if (!String.Equals(model.Name, model.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["The name contains invalid characters."]);
                }

                var secretBindings = await _secretCoordinator.LoadSecretBindingsAsync();

                // Do not check the stores as a readonly store would already have the key value.
                if (secretBindings.ContainsKey(model.Name))
                {
                    ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["A secret with the same name already exists."]);
                }
            }

            dynamic editor = await _displayManager.UpdateEditorAsync(secret, updater: _updateModelAccessor.ModelUpdater, isNew: true);
            editor.Secret = secret;

            if (ModelState.IsValid)
            {
                var secretBinding = new SecretBinding { Store = model.SelectedStore, Description = model.Description, Type = model.Type };

                secret.Id = model.SecretId;
                secret.Name = model.Name;

                await _secretCoordinator.UpdateSecretAsync(model.Name, secretBinding, secret);

                _notifier.Success(H["Secret added successfully"]);

                return RedirectToAction(nameof(Index));
            }

            model.Editor = editor;
            model.StoreEntries = _secretCoordinator.ToArray();

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> Edit(string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var secretBindings = await _secretCoordinator.GetSecretBindingsAsync();

            if (!secretBindings.ContainsKey(name))
            {
                return RedirectToAction(nameof(Create), new { name });
            }

            var secretBinding = secretBindings[name];

            var secret = _factories.FirstOrDefault(x => x.Name == secretBinding.Type)?.Create();
            secret = await _secretCoordinator.GetSecretAsync(name, secret.GetType());

            var model = new SecretBindingViewModel
            {
                Name = name,
                SelectedStore = secretBinding.Store,
                Description = secretBinding.Description,
                Type = secret.GetType().Name,
                Secret = secret,
                StoreEntries = _secretCoordinator.ToArray(),
                Editor = await _displayManager.BuildEditorAsync(secret, updater: _updateModelAccessor.ModelUpdater, isNew: false)
            };

            model.Editor.Secret = secret;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string sourceName, SecretBindingViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var secretBindings = await _secretCoordinator.LoadSecretBindingsAsync();

            if (ModelState.IsValid)
            {
                if (String.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["The name is mandatory."]);
                }
                if (!model.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.Equals(model.Name, model.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["The name contains invalid characters."]);
                    }
                    if (secretBindings.ContainsKey(model.Name))
                    {
                        ModelState.AddModelError(nameof(SecretBindingViewModel.Name), S["A secret with the same name already exists."]);
                    }
                }
            }

            if (!secretBindings.ContainsKey(sourceName))
            {
                return NotFound();
            }

            var secretBinding = secretBindings[sourceName];

            var secret = _factories.FirstOrDefault(x => x.Name == secretBinding.Type)?.Create();
            secret = await _secretCoordinator.GetSecretAsync(sourceName, secret.GetType());

            var editor = await _displayManager.UpdateEditorAsync(secret, updater: _updateModelAccessor.ModelUpdater, isNew: false);
            model.Editor = editor;

            if (ModelState.IsValid)
            {
                // Remove this before updating the binding value.
                await _secretCoordinator.RemoveSecretAsync(sourceName, secretBinding.Store);
                secretBinding.Store = model.SelectedStore;
                secretBinding.Description = model.Description;
                secret.Name = model.Name;
                await _secretCoordinator.UpdateSecretAsync(model.Name, secretBinding, secret);

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSecrets))
            {
                return Forbid();
            }

            var secretBindings = await _secretCoordinator.GetSecretBindingsAsync();

            if (!secretBindings.ContainsKey(name))
            {
                return NotFound();
            }

            var secretBinding = secretBindings[name];

            await _secretCoordinator.RemoveSecretAsync(name, secretBinding.Store);

            _notifier.Success(H["Secret deleted successfully"]);

            return RedirectToAction(nameof(Index));
        }
    }
}
