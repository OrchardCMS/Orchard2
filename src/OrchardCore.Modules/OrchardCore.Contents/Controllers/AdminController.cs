using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Contents.Services;
using OrchardCore.Contents.ViewModels;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Contents.Controllers
{
    public class AdminController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ISiteService _siteService;
        private readonly YesSql.ISession _session;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly INotifier _notifier;
        private readonly IAuthorizationService _authorizationService;
        private readonly IDisplayManager<ContentOptionsViewModel> _contentOptionsDisplayManager;
        private readonly IContentsAdminListQueryService _contentsAdminListQueryService;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IShapeFactory _shapeFactory;
        private readonly dynamic New;
        private readonly ILogger _logger;

        private readonly ITypeActivatorFactory<ContentPart> _contentPartFactory;
        private readonly ITypeActivatorFactory<ContentField> _contentFieldFactory;

        public AdminController(
            IAuthorizationService authorizationService,
            IContentManager contentManager,
            IContentItemDisplayManager contentItemDisplayManager,
            IContentDefinitionManager contentDefinitionManager,
            ISiteService siteService,
            INotifier notifier,
            YesSql.ISession session,
            IShapeFactory shapeFactory,
            IDisplayManager<ContentOptionsViewModel> contentOptionsDisplayManager,
            IContentsAdminListQueryService contentsAdminListQueryService,
            ILogger<AdminController> logger,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            IStringLocalizer<AdminController> stringLocalizer,
            IUpdateModelAccessor updateModelAccessor,
            IHttpContextAccessor httpContextAccessor,
            ITypeActivatorFactory<ContentPart> contentPartFactory,
            ITypeActivatorFactory<ContentField> contentFieldFactory)
        {
            _authorizationService = authorizationService;
            _notifier = notifier;
            _contentItemDisplayManager = contentItemDisplayManager;
            _session = session;
            _siteService = siteService;
            _contentManager = contentManager;
            _contentDefinitionManager = contentDefinitionManager;
            _updateModelAccessor = updateModelAccessor;
            _httpContextAccessor = httpContextAccessor;
            _contentOptionsDisplayManager = contentOptionsDisplayManager;
            _contentsAdminListQueryService = contentsAdminListQueryService;

            H = htmlLocalizer;
            S = stringLocalizer;
            _shapeFactory = shapeFactory;
            New = shapeFactory;
            _logger = logger;
            _contentPartFactory = contentPartFactory;
            _contentFieldFactory = contentFieldFactory;
        }

        [HttpGet]
        public async Task<IActionResult> List(ListContentsViewModel model, PagerParameters pagerParameters, string contentTypeId = "")
        {
            var context = _httpContextAccessor.HttpContext;
            var contentTypeDefinitions = _contentDefinitionManager.ListTypeDefinitions()
                    .Where(ctd => ctd.GetSettings<ContentTypeSettings>().Creatable)
                    .OrderBy(ctd => ctd.DisplayName);

            if (!await _authorizationService.AuthorizeContentTypeDefinitionsAsync(User, CommonPermissions.EditContent, contentTypeDefinitions, _contentManager))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);

            // This is used by the AdminMenus so needs to be passed into the options.
            if (!String.IsNullOrEmpty(contentTypeId))
            {
                model.Options.SelectedContentType = contentTypeId;
            }

            // Populate the creatable types.
            if (!String.IsNullOrEmpty(model.Options.SelectedContentType))
            {
                var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(model.Options.SelectedContentType);
                if (contentTypeDefinition == null)
                {
                    return NotFound();
                }

                var creatableList = new List<SelectListItem>();

                // Allows non creatable types to be created by another admin page.
                if (contentTypeDefinition.GetSettings<ContentTypeSettings>().Creatable || model.Options.CanCreateSelectedContentType)
                {
                    creatableList.Add(new SelectListItem(new LocalizedString(contentTypeDefinition.DisplayName, contentTypeDefinition.DisplayName).Value, contentTypeDefinition.Name));
                }

                model.Options.CreatableTypes = creatableList;
            }

            if (model.Options.CreatableTypes == null)
            {
                var creatableList = new List<SelectListItem>();
                if (contentTypeDefinitions.Any())
                {
                    foreach (var contentTypeDefinition in contentTypeDefinitions)
                    {
                        var contentItem = await _contentManager.NewAsync(contentTypeDefinition.Name);
                        contentItem.Owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (await _authorizationService.AuthorizeAsync(context.User, CommonPermissions.EditContent, contentItem))
                        {
                            creatableList.Add(new SelectListItem(new LocalizedString(contentTypeDefinition.DisplayName, contentTypeDefinition.DisplayName).Value, contentTypeDefinition.Name));
                        }
                    }
                }

                model.Options.CreatableTypes = creatableList;
            }

            // We populate the remaining SelectLists.
            model.Options.ContentStatuses = new List<SelectListItem>()
            {
                new SelectListItem() { Text = S["Latest"], Value = nameof(ContentsStatus.Latest) },
                new SelectListItem() { Text = S["Published"], Value = nameof(ContentsStatus.Published) },
                new SelectListItem() { Text = S["Unpublished"], Value = nameof(ContentsStatus.Draft) },
                new SelectListItem() { Text = S["All versions"], Value = nameof(ContentsStatus.AllVersions) }
            };

            if (await _authorizationService.AuthorizeAsync(context.User, Permissions.ListContent))
            {
                model.Options.ContentStatuses.Insert(1, new SelectListItem() { Text = S["Owned by me"], Value = nameof(ContentsStatus.Owner) });
            }

            model.Options.ContentSorts = new List<SelectListItem>()
            {
                new SelectListItem() { Text = S["Recently created"], Value = nameof(ContentsOrder.Created) },
                new SelectListItem() { Text = S["Recently modified"], Value = nameof(ContentsOrder.Modified) },
                new SelectListItem() { Text = S["Recently published"], Value = nameof(ContentsOrder.Published) },
                new SelectListItem() { Text = S["Title"], Value = nameof(ContentsOrder.Title) }
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>()
            {
                new SelectListItem() { Text = S["Publish Now"], Value = nameof(ContentsBulkAction.PublishNow) },
                new SelectListItem() { Text = S["Unpublish"], Value = nameof(ContentsBulkAction.Unpublish) },
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            if ((String.IsNullOrEmpty(model.Options.SelectedContentType) || String.IsNullOrEmpty(contentTypeId)) && model.Options.ContentTypeOptions == null)
            {
                var listableTypes = new List<ContentTypeDefinition>();
                foreach (var ctd in _contentDefinitionManager.ListTypeDefinitions())
                {
                    if (ctd.GetSettings<ContentTypeSettings>().Listable)
                    {
                        var contentItem = await _contentManager.NewAsync(ctd.Name);
                        contentItem.Owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        var authorized = await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem);

                        if (authorized)
                        {
                            listableTypes.Add(ctd);
                        }
                    }
                }

                var contentTypeOptions = listableTypes
                    .Select(ctd => new KeyValuePair<string, string>(ctd.Name, ctd.DisplayName))
                    .ToList().OrderBy(kvp => kvp.Value);

                model.Options.ContentTypeOptions = new List<SelectListItem>
                {
                    new SelectListItem() { Text = S["All content types"], Value = "" }
                };

                foreach (var option in contentTypeOptions)
                {
                    model.Options.ContentTypeOptions.Add(new SelectListItem() { Text = option.Value, Value = option.Key, Selected = (option.Value == model.Options.SelectedContentType) });
                }
            }

            // If ContentTypeOptions is not initialized by query string or by the code above, initialize it
            if (model.Options.ContentTypeOptions == null)
            {
                model.Options.ContentTypeOptions = new List<SelectListItem>();
            }

            // With the model populated we filter the query, allowing the filters to alter the model.
            var query = await _contentsAdminListQueryService.QueryAsync(model.Options, _updateModelAccessor.ModelUpdater);

            var maxPagedCount = siteSettings.MaxPagedCount;
            if (maxPagedCount > 0 && pager.PageSize > maxPagedCount)
            {
                pager.PageSize = maxPagedCount;
            }

            // Populate route values to maintain previous route data when generating page links.
            await _contentOptionsDisplayManager.UpdateEditorAsync(model.Options, _updateModelAccessor.ModelUpdater, false);

            var routeData = new RouteData(model.Options.RouteValues);

            var pagerShape = (await New.Pager(pager)).TotalItemCount(maxPagedCount > 0 ? maxPagedCount : await query.CountAsync()).RouteData(routeData);

            // Load items so that loading handlers are invoked.
            var pageOfContentItems = await query.Skip(pager.GetStartIndex()).Take(pager.PageSize).ListAsync(_contentManager);

            // We prepare the content items SummaryAdmin shape
            var contentItemSummaries = new List<dynamic>();
            foreach (var contentItem in pageOfContentItems)
            {
                contentItemSummaries.Add(await _contentItemDisplayManager.BuildDisplayAsync(contentItem, _updateModelAccessor.ModelUpdater, "SummaryAdmin"));
            }

            // Populate options pager summary values.
            var startIndex = (pagerShape.Page - 1) * (pagerShape.PageSize) + 1;
            model.Options.StartIndex = startIndex;
            model.Options.EndIndex = startIndex + contentItemSummaries.Count - 1;
            model.Options.ContentItemsCount = contentItemSummaries.Count;
            model.Options.TotalItemCount = pagerShape.TotalItemCount;

            var header = await _contentOptionsDisplayManager.BuildEditorAsync(model.Options, _updateModelAccessor.ModelUpdater, false);

            var shapeViewModel = await _shapeFactory.CreateAsync<ListContentsViewModel>("ContentsAdminList", viewModel =>
            {
                viewModel.ContentItems = contentItemSummaries;
                viewModel.Pager = pagerShape;
                viewModel.Options = model.Options;
                viewModel.Header = header;
            });

            return View(shapeViewModel);
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("submit.Filter")]
        public async Task<ActionResult> ListFilterPOST(ListContentsViewModel model)
        {

            await _contentOptionsDisplayManager.UpdateEditorAsync(model.Options, _updateModelAccessor.ModelUpdater, false);

            return RedirectToAction(nameof(List), model.Options.RouteValues);
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> ListPOST(ContentOptionsViewModel options, IEnumerable<int> itemIds)
        {
            if (itemIds?.Count() > 0)
            {
                // Load items so that loading handlers are invoked.
                var checkedContentItems = await _session.Query<ContentItem, ContentItemIndex>().Where(x => x.DocumentId.IsIn(itemIds) && x.Latest).ListAsync(_contentManager);
                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.PublishNow:
                        foreach (var item in checkedContentItems)
                        {
                            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, item))
                            {
                                _notifier.Warning(H["Couldn't publish selected content."]);
                                await _session.CancelAsync();
                                return Forbid();
                            }

                            await _contentManager.PublishAsync(item);
                        }
                        _notifier.Success(H["Content published successfully."]);
                        break;
                    case ContentsBulkAction.Unpublish:
                        foreach (var item in checkedContentItems)
                        {
                            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, item))
                            {
                                _notifier.Warning(H["Couldn't unpublish selected content."]);
                                await _session.CancelAsync();
                                return Forbid();
                            }

                            await _contentManager.UnpublishAsync(item);
                        }
                        _notifier.Success(H["Content unpublished successfully."]);
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in checkedContentItems)
                        {
                            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.DeleteContent, item))
                            {
                                _notifier.Warning(H["Couldn't remove selected content."]);
                                await _session.CancelAsync();
                                return Forbid();
                            }

                            await _contentManager.RemoveAsync(item);
                        }
                        _notifier.Success(H["Content removed successfully."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Create(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var contentItem = await _contentManager.NewAsync(id);

            // Set the current user as the owner to check for ownership permissions on creation
            contentItem.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem))
            {
                return Forbid();
            }

            var model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, true);

            return View(model);
        }

        [HttpPost, ActionName("Create")]
        [FormValueRequired("submit.Save")]
        public Task<IActionResult> CreatePOST(string id, [Bind(Prefix = "submit.Save")] string submitSave, string returnUrl)
        {
            var stayOnSamePage = submitSave == "submit.SaveAndContinue";
            return CreatePOST(id, returnUrl, stayOnSamePage, async contentItem =>
            {
                await _contentManager.SaveDraftAsync(contentItem);

                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["Your content draft has been saved."]
                    : H["Your {0} draft has been saved.", typeDefinition.DisplayName]);
            });
        }

        [HttpPost, ActionName("Create")]
        [FormValueRequired("submit.Publish")]
        public async Task<IActionResult> CreateAndPublishPOST(string id, [Bind(Prefix = "submit.Publish")] string submitPublish, string returnUrl)
        {
            var stayOnSamePage = submitPublish == "submit.PublishAndContinue";
            // pass a dummy content to the authorization check to check for "own" variations
            var dummyContent = await _contentManager.NewAsync(id);

            // Set the current user as the owner to check for ownership permissions on creation
            dummyContent.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, dummyContent))
            {
                return Forbid();
            }

            return await CreatePOST(id, returnUrl, stayOnSamePage, async contentItem =>
            {
                await _contentManager.PublishAsync(contentItem);

                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["Your content has been published."]
                    : H["Your {0} has been published.", typeDefinition.DisplayName]);
            });
        }

        private async Task<IActionResult> CreatePOST(string id, string returnUrl, bool stayOnSamePage, Func<ContentItem, Task> conditionallyPublish)
        {
            var contentItem = await _contentManager.NewAsync(id);

            // Set the current user as the owner to check for ownership permissions on creation
            contentItem.Owner = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem))
            {
                return Forbid();
            }

            var model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, true);

            if (ModelState.IsValid)
            {
                await _contentManager.CreateAsync(contentItem, VersionOptions.Draft);
            }

            if (!ModelState.IsValid)
            {
                await _session.CancelAsync();
                return View(model);
            }

            await conditionallyPublish(contentItem);

            if ((!string.IsNullOrEmpty(returnUrl)) && (!stayOnSamePage))
            {
                return LocalRedirect(returnUrl);
            }

            var adminRouteValues = (await _contentManager.PopulateAspectAsync<ContentItemMetadata>(contentItem)).AdminRouteValues;

            if (!string.IsNullOrEmpty(returnUrl))
            {
                adminRouteValues.Add("returnUrl", returnUrl);
            }

            return RedirectToRoute(adminRouteValues);
        }

        public async Task<IActionResult> Display(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.ViewContent, contentItem))
            {
                return Forbid();
            }

            var model = await _contentItemDisplayManager.BuildDisplayAsync(contentItem, _updateModelAccessor.ModelUpdater, "DetailAdmin");

            return View(model);
        }

        public async Task<IActionResult> Edit(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem))
            {
                return Forbid();
            }

            var model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);

            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("submit.Save")]
        public Task<IActionResult> EditPOST(string contentItemId, [Bind(Prefix = "submit.Save")] string submitSave, string returnUrl)
        {
            var stayOnSamePage = submitSave == "submit.SaveAndContinue";
            return EditPOST(contentItemId, returnUrl, stayOnSamePage, async contentItem =>
            {
                await _contentManager.SaveDraftAsync(contentItem);

                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["Your content draft has been saved."]
                    : H["Your {0} draft has been saved.", typeDefinition.DisplayName]);
            });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("submit.Publish")]
        public async Task<IActionResult> EditAndPublishPOST(string contentItemId, [Bind(Prefix = "submit.Publish")] string submitPublish, string returnUrl)
        {
            var stayOnSamePage = submitPublish == "submit.PublishAndContinue";

            var content = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (content == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, content))
            {
                return Forbid();
            }
            return await EditPOST(contentItemId, returnUrl, stayOnSamePage, async contentItem =>
            {
                await _contentManager.PublishAsync(contentItem);

                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["Your content has been published."]
                    : H["Your {0} has been published.", typeDefinition.DisplayName]);
            });
        }

        private async Task<IActionResult> EditPOST(string contentItemId, string returnUrl, bool stayOnSamePage, Func<ContentItem, Task> conditionallyPublish)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem))
            {
                return Forbid();
            }

            var model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);

            if (!ModelState.IsValid)
            {
                await _session.CancelAsync();
                return View(nameof(Edit), model);
            }

            await conditionallyPublish(contentItem);

            if (returnUrl == null)
            {
                return RedirectToAction(nameof(Edit), new RouteValueDictionary { { "ContentItemId", contentItem.ContentItemId } });
            }
            else if (stayOnSamePage)
            {
                return RedirectToAction(nameof(Edit), new RouteValueDictionary { { "ContentItemId", contentItem.ContentItemId }, { "returnUrl", returnUrl } });
            }
            else
            {
                return LocalRedirect(returnUrl);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Clone(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.CloneContent, contentItem))
            {
                return Forbid();
            }

            try
            {
                await _contentManager.CloneAsync(contentItem);
            }
            catch (InvalidOperationException)
            {
                _notifier.Warning(H["Could not clone the content item."]);
                return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
            }

            _notifier.Information(H["Successfully cloned. The clone was saved as a draft."]);

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> DiscardDraft(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null || contentItem.Published)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.DeleteContent, contentItem))
            {
                return Forbid();
            }

            if (contentItem != null)
            {
                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                await _contentManager.DiscardDraftAsync(contentItem);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["The draft has been removed."]
                    : H["The {0} draft has been removed.", typeDefinition.DisplayName]);
            }

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.DeleteContent, contentItem))
            {
                return Forbid();
            }

            if (contentItem != null)
            {
                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                await _contentManager.RemoveAsync(contentItem);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["That content has been removed."]
                    : H["That {0} has been removed.", typeDefinition.DisplayName]);
            }

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> Publish(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, contentItem))
            {
                return Forbid();
            }

            await _contentManager.PublishAsync(contentItem);

            var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

            if (string.IsNullOrEmpty(typeDefinition.DisplayName))
            {
                _notifier.Success(H["That content has been published."]);
            }
            else
            {
                _notifier.Success(H["That {0} has been published.", typeDefinition.DisplayName]);
            }

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> Unpublish(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PublishContent, contentItem))
            {
                return Forbid();
            }

            await _contentManager.UnpublishAsync(contentItem);

            var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

            if (string.IsNullOrEmpty(typeDefinition.DisplayName))
            {
                _notifier.Success(H["The content has been unpublished."]);
            }
            else
            {
                _notifier.Success(H["The {0} has been unpublished.", typeDefinition.DisplayName]);
            }

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction(nameof(List));
        }


        [HttpGet]
        public async Task<IActionResult> GetContentFormat(string contentTypeName)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(contentTypeName);
            if (contentTypeDefinition == null)
            {
                return NotFound();
            }
            var contentItem = await _contentManager.NewAsync(contentTypeName);
            contentItem.Owner =User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.EditContent, contentItem))
            {
                return Unauthorized();
            }
            return Json(contentItem);
        }
    }
}
