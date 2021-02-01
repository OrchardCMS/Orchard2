using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Documents;
using OrchardCore.Entities;
using OrchardCore.Layers.Handlers;
using OrchardCore.Layers.Models;
using OrchardCore.Layers.Services;
using OrchardCore.Layers.ViewModels;
using OrchardCore.Settings;
using YesSql;

namespace OrchardCore.Layers.Controllers
{
    public class AdminController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly ISiteService _siteService;
        private readonly ILayerService _layerService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISession _session;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IVolatileDocumentManager<LayerState> _layerStateManager;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly INotifier _notifier;

        public AdminController(
            IContentManager contentManager,
            IContentItemDisplayManager contentItemDisplayManager,
            ISiteService siteService,
            ILayerService layerService,
            IAuthorizationService authorizationService,
            ISession session,
            IUpdateModelAccessor updateModelAccessor,
            IVolatileDocumentManager<LayerState> layerStateManager,
            IStringLocalizer<AdminController> stringLocalizer,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            INotifier notifier)
        {
            _contentManager = contentManager;
            _contentItemDisplayManager = contentItemDisplayManager;
            _siteService = siteService;
            _layerService = layerService;
            _authorizationService = authorizationService;
            _session = session;
            _updateModelAccessor = updateModelAccessor;
            _layerStateManager = layerStateManager;
            _notifier = notifier;
            S = stringLocalizer;
            H = htmlLocalizer;
        }

        public async Task<IActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            var layers = await _layerService.GetLayersAsync();
            var widgets = await _layerService.GetLayerWidgetsMetadataAsync(c => c.Latest == true);

            var model = new LayersIndexViewModel { Layers = layers.Layers.ToList() };

            var siteSettings = await _siteService.GetSiteSettingsAsync();

            model.Zones = siteSettings.As<LayerSettings>().Zones ?? Array.Empty<string>();
            model.Widgets = new Dictionary<string, List<dynamic>>();

            foreach (var widget in widgets.OrderBy(x => x.Position))
            {
                var zone = widget.Zone;
                List<dynamic> list;
                if (!model.Widgets.TryGetValue(zone, out list))
                {
                    model.Widgets.Add(zone, list = new List<dynamic>());
                }

                list.Add(await _contentItemDisplayManager.BuildDisplayAsync(widget.ContentItem, _updateModelAccessor.ModelUpdater, "SummaryAdmin"));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(LayersIndexViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Create(string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            return View();
        }

        [HttpPost, ActionName("Create")]
        public async Task<IActionResult> CreatePost(LayerEditViewModel model, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            var layers = await _layerService.LoadLayersAsync();

            ValidateViewModel(model, layers, isNew: true);

            if (ModelState.IsValid)
            {
                layers.Layers.Add(new Layer
                {
                    Name = model.Name,
                    Rule = model.Rule,
                    Description = model.Description
                });

                await _layerService.UpdateAsync(layers);

                return RedirectToReturnUrlOrIndex(returnUrl);
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string name, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            var layers = await _layerService.GetLayersAsync();

            var layer = layers.Layers.FirstOrDefault(x => String.Equals(x.Name, name));

            if (layer == null)
            {
                return NotFound();
            }

            var model = new LayerEditViewModel
            {
                Name = layer.Name,
                Rule = layer.Rule,
                Description = layer.Description
            };

            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> EditPost(LayerEditViewModel model, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            var layers = await _layerService.LoadLayersAsync();

            ValidateViewModel(model, layers, isNew: false);

            if (ModelState.IsValid)
            {
                var layer = layers.Layers.FirstOrDefault(x => String.Equals(x.Name, model.Name));

                if (layer == null)
                {
                    return NotFound();
                }

                layer.Name = model.Name;
                layer.Rule = model.Rule;
                layer.Description = model.Description;

                await _layerService.UpdateAsync(layers);

                return RedirectToReturnUrlOrIndex(returnUrl);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string name, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return Forbid();
            }

            var layers = await _layerService.LoadLayersAsync();

            var layer = layers.Layers.FirstOrDefault(x => String.Equals(x.Name, name));

            if (layer == null)
            {
                return NotFound();
            }

            var widgets = await _layerService.GetLayerWidgetsMetadataAsync(c => c.Latest == true);

            if (!widgets.Any(x => String.Equals(x.Layer, name, StringComparison.OrdinalIgnoreCase)))
            {
                layers.Layers.Remove(layer);
                await _layerService.UpdateAsync(layers);
                _notifier.Success(H["Layer deleted successfully."]);
            }
            else
            {
                _notifier.Error(H["The layer couldn't be deleted: you must remove any associated widgets first."]);
            }

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePosition(string contentItemId, double position, string zone, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageLayers))
            {
                return StatusCode(401);
            }

            // Load the latest version first if any
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
            {
                return StatusCode(404);
            }

            var layerMetadata = contentItem.As<LayerMetadata>();

            if (layerMetadata == null)
            {
                return StatusCode(403);
            }

            layerMetadata.Position = position;
            layerMetadata.Zone = zone;

            contentItem.Apply(layerMetadata);

            _session.Save(contentItem);

            // In case the moved contentItem is the draft for a published contentItem we update it's position too.
            // We do that because we want the position of published and draft version to be the same.
            if (contentItem.IsPublished() == false)
            {
                var publishedContentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Published);
                if (publishedContentItem != null)
                {
                    layerMetadata = contentItem.As<LayerMetadata>();

                    if (layerMetadata == null)
                    {
                        return StatusCode(403);
                    }

                    layerMetadata.Position = position;
                    layerMetadata.Zone = zone;

                    publishedContentItem.Apply(layerMetadata);

                    _session.Save(publishedContentItem);
                }
            }

            // The state will be updated once the ambient session is committed.
            await _layerStateManager.UpdateAsync(new LayerState());

            if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return StatusCode(200);
            }
            else
            {
                return RedirectToReturnUrlOrIndex(returnUrl);
            }
        }

        private void ValidateViewModel(LayerEditViewModel model, LayersDocument layers, bool isNew)
        {
            if (String.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(LayerEditViewModel.Name), S["The layer name is required."]);
            }
            else if (isNew && layers.Layers.Any(x => String.Equals(x.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(LayerEditViewModel.Name), S["The layer name already exists."]);
            }

            if (String.IsNullOrWhiteSpace(model.Rule))
            {
                ModelState.AddModelError(nameof(LayerEditViewModel.Rule), S["The rule is required."]);
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
