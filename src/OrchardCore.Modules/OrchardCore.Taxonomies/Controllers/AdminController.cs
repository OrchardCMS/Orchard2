using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Services;
using OrchardCore.Taxonomies.ViewModels;
using YesSql;

namespace OrchardCore.Taxonomies.Controllers
{
    public class AdminController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ISession _session;
        private readonly IHtmlLocalizer H;
        private readonly INotifier _notifier;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly ITaxonomyService _taxonomyService;
        private readonly ISiteService _siteService;
        private readonly dynamic New;

        public AdminController(
            ISession session,
            IContentManager contentManager,
            IAuthorizationService authorizationService,
            IContentItemDisplayManager contentItemDisplayManager,
            IContentDefinitionManager contentDefinitionManager,
            INotifier notifier,
            IHtmlLocalizer<AdminController> localizer,
            IUpdateModelAccessor updateModelAccessor,
            ITaxonomyService taxonomyService,
            ISiteService siteService,
            IShapeFactory shapeFactory)
        {
            _contentManager = contentManager;
            _authorizationService = authorizationService;
            _contentItemDisplayManager = contentItemDisplayManager;
            _contentDefinitionManager = contentDefinitionManager;
            _session = session;
            _notifier = notifier;
            _updateModelAccessor = updateModelAccessor;
            _taxonomyService = taxonomyService;
            _siteService = siteService;
            New = shapeFactory;
            H = localizer;
        }

        public async Task<IActionResult> Create(string id, string taxonomyContentItemId, string taxonomyItemId)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            var contentItem = await _contentManager.NewAsync(id);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, true);

            model.TaxonomyContentItemId = taxonomyContentItemId;
            model.TaxonomyItemId = taxonomyItemId;

            return View(model);
        }

        [HttpPost]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(string id, string taxonomyContentItemId, string taxonomyItemId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition("Taxonomy");

            if (!contentTypeDefinition.GetSettings<ContentTypeSettings>().Draftable)
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            var contentItem = await _contentManager.NewAsync(id);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, true);

            if (!ModelState.IsValid)
            {
                model.TaxonomyContentItemId = taxonomyContentItemId;
                model.TaxonomyItemId = taxonomyItemId;

                return View(model);
            }

            if (taxonomyItemId == null)
            {
                // Use the taxonomy as the parent if no target is specified
                taxonomy.Alter<TaxonomyPart>(part => part.Terms.Add(contentItem));
            }
            else
            {
                // Look for the target taxonomy item in the hierarchy
                var parentTaxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

                // Couldn't find targeted taxonomy item
                if (parentTaxonomyItem == null)
                {
                    return NotFound();
                }

                var taxonomyItems = parentTaxonomyItem?.Terms as JArray;

                if (taxonomyItems == null)
                {
                    parentTaxonomyItem["Terms"] = taxonomyItems = new JArray();
                }

                taxonomyItems.Add(JObject.FromObject(contentItem));
            }

            _session.Save(taxonomy);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        public async Task<IActionResult> Edit(string taxonomyContentItemId, string taxonomyItemId)
        {
            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies, taxonomy))
            {
                return Forbid();
            }

            // Look for the target taxonomy item in the hierarchy
            JObject taxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var contentItem = taxonomyItem.ToObject<ContentItem>();
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);

            model.TaxonomyContentItemId = taxonomyContentItemId;
            model.TaxonomyItemId = taxonomyItemId;

            return View(model);
        }

        [HttpPost]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost(string taxonomyContentItemId, string taxonomyItemId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition("Taxonomy");

            if (!contentTypeDefinition.GetSettings<ContentTypeSettings>().Draftable)
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            // Look for the target taxonomy item in the hierarchy
            JObject taxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var existing = taxonomyItem.ToObject<ContentItem>();

            // Create a new item to take into account the current type definition.
            var contentItem = await _contentManager.NewAsync(existing.ContentType);

            contentItem.ContentItemId = existing.ContentItemId;
            contentItem.Merge(existing);
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            dynamic model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);

            if (!ModelState.IsValid)
            {
                model.TaxonomyContentItemId = taxonomyContentItemId;
                model.TaxonomyItemId = taxonomyItemId;

                return View(model);
            }

            taxonomyItem.Merge(contentItem.Content, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            // Merge doesn't copy the properties
            taxonomyItem[nameof(ContentItem.DisplayText)] = contentItem.DisplayText;

            _session.Save(taxonomy);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string taxonomyContentItemId, string taxonomyItemId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies))
            {
                return Forbid();
            }

            ContentItem taxonomy;

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition("Taxonomy");

            if (!contentTypeDefinition.GetSettings<ContentTypeSettings>().Draftable)
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);
            }
            else
            {
                taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.DraftRequired);
            }

            if (taxonomy == null)
            {
                return NotFound();
            }

            // Look for the target taxonomy item in the hierarchy
            var taxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            // Couldn't find targeted taxonomy item
            if (taxonomyItem == null)
            {
                return NotFound();
            }

            taxonomyItem.Remove();
            _session.Save(taxonomy);

            _notifier.Success(H["Taxonomy item deleted successfully."]);

            return RedirectToAction(nameof(Edit), "Admin", new { area = "OrchardCore.Contents", contentItemId = taxonomyContentItemId });
        }

        public async Task<IActionResult> ListCategorizedContentItems(string taxonomyContentItemId, string taxonomyItemId)
        {
            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies, taxonomy))
            {
                return Forbid();
            }

            var taxonomyPart = taxonomy.As<TaxonomyPart>();

            JObject taxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var contentItem = taxonomyItem.ToObject<ContentItem>();
            contentItem.Weld<TermPart>();
            contentItem.Alter<TermPart>(t => t.TaxonomyContentItemId = taxonomyContentItemId);

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pagerParameters = new PagerSlimParameters();
            await _updateModelAccessor.ModelUpdater.TryUpdateModelAsync(pagerParameters);
            var pager = new PagerSlim(pagerParameters, siteSettings.PageSize);

            var model = new TermPartViewModel();
            var termPart = contentItem.As<TermPart>();
            model.TaxonomyContentItemId = termPart.TaxonomyContentItemId;
            model.ContentItem = termPart.ContentItem;
            model.ContentItems = (await _taxonomyService.QueryCategorizedItemsAsync(termPart, pager, taxonomyPart.EnableOrdering, false)).ToArray();
            model.Pager = await New.PagerSlim(pager);
            model.EnableOrdering = taxonomyPart.EnableOrdering;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ListCategorizedContentItemsPost(string taxonomyContentItemId, string taxonomyItemId, int oldIndex, int newIndex, PagerSlimParameters pagerSlimParameters, int pageSize)
        {
            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTaxonomies, taxonomy))
            {
                return Forbid();
            }

            JObject taxonomyItem = _taxonomyService.FindTaxonomyItem(taxonomy.As<TaxonomyPart>().Content, taxonomyItemId);

            if (taxonomyItem == null)
            {
                return NotFound();
            }

            var contentItem = taxonomyItem.ToObject<ContentItem>();

            contentItem.Weld<TermPart>();

            var termPart = contentItem.As<TermPart>();

            var pager = new PagerSlim(pagerSlimParameters, pageSize);

            var categorizedContentItems = (await _taxonomyService.QueryCategorizedItemsAsync(termPart, pager, true, false)).ToList();

            // Find the lower and higher bounds of the affected area (between the old and new position of the moved item)
            var lowerIndex = Math.Min(newIndex, oldIndex);
            var higherIndex = Math.Max(newIndex, oldIndex);

            // Find the lower order value
            var lowerOrderValue = _taxonomyService.GetTaxonomyTermOrder(categorizedContentItems[higherIndex], taxonomyItemId);

            // Move the element to it's new position
            var categorizedContentItem = categorizedContentItems[oldIndex];
            categorizedContentItems.Remove(categorizedContentItem);
            categorizedContentItems.Insert(newIndex, categorizedContentItem);

            // Restrict the list to the elements whose order value needs to be updated
            categorizedContentItems = categorizedContentItems.GetRange(lowerIndex, higherIndex - lowerIndex + 1);

            // Apply and save the new order values
            var orderValue = lowerOrderValue;
            categorizedContentItems.Reverse();
            foreach (var categorizedItem in categorizedContentItems)
            {
                _taxonomyService.RegisterCategorizedItemOrder(categorizedItem, taxonomyItemId, orderValue);

                // If this instance of the item is not published, but a published version exists, make sure that both versions appear at the same position for this term
                if (!categorizedItem.Published)
                {
                    var publishedCategorizedItem = await _contentManager.GetAsync(categorizedItem.ContentItemId, VersionOptions.Published);
                    if (publishedCategorizedItem != null)
                    {
                        _taxonomyService.RegisterCategorizedItemOrder(publishedCategorizedItem, taxonomyItemId, orderValue);
                    }
                }

                ++orderValue;
            }

            return Ok();
        }
    }
}
