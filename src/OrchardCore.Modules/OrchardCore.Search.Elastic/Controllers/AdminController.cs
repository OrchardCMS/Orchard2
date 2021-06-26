using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Liquid;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Search.Elastic.ViewModels;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Search.Elastic.Services;
using OrchardCore.Settings;
using YesSql;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Search.Elastic.Controllers
{
    public class AdminController : Controller
    {
        private readonly ISession _session;
        private readonly ElasticIndexManager _elasticIndexManager;
        private readonly ElasticIndexingService _elasticIndexingService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotifier _notifier;
        private readonly ElasticAnalyzerManager _elasticAnalyzerManager;
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;
        private readonly IElasticQueryService _queryService;
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ISiteService _siteService;
        private readonly dynamic New;
        private readonly JavaScriptEncoder _javaScriptEncoder;
        private readonly IStringLocalizer S;
        private readonly IHtmlLocalizer H;
        private readonly ILogger _logger;
        private readonly IOptions<TemplateOptions> _templateOptions;
        private readonly ShellSettings _shellSettings;

        public AdminController(
            ISession session,
            IContentDefinitionManager contentDefinitionManager,
            ElasticIndexManager elasticIndexManager,
            ElasticIndexingService elasticIndexingService,
            IAuthorizationService authorizationService,
            ElasticAnalyzerManager elasticAnalyzerManager,
            ElasticIndexSettingsService elasticIndexSettingsService,
            IElasticQueryService queryService,
            ILiquidTemplateManager liquidTemplateManager,
            INotifier notifier,
            ISiteService siteService,
            IShapeFactory shapeFactory,
            JavaScriptEncoder javaScriptEncoder,
            IStringLocalizer<AdminController> stringLocalizer,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            ILogger<AdminController> logger,
            IOptions<TemplateOptions> templateOptions,
            ShellSettings shellSettings
            )
        {
            _session = session;
            _elasticIndexManager = elasticIndexManager;
            _elasticIndexingService = elasticIndexingService;
            _authorizationService = authorizationService;
            _elasticAnalyzerManager = elasticAnalyzerManager;
            _elasticIndexSettingsService = elasticIndexSettingsService;
            _queryService = queryService;
            _liquidTemplateManager = liquidTemplateManager;
            _contentDefinitionManager = contentDefinitionManager;
            _notifier = notifier;
            _siteService = siteService;

            New = shapeFactory;
            _javaScriptEncoder = javaScriptEncoder;
            S = stringLocalizer;
            H = htmlLocalizer;
            _logger = logger;
            _templateOptions = templateOptions;
            _shellSettings = shellSettings;
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            var indexes = (await _elasticIndexSettingsService.GetSettingsAsync()).Select(i => new IndexViewModel { Name = i.IndexName });

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);
            var count = indexes.Count();
            var results = indexes;

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                results = results.Where(q => q.Name.IndexOf(options.Search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            results = results
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize).ToList();

            // Maintain previous route data when generating page links
            var routeData = new RouteData();
            var pagerShape = (await New.Pager(pager)).TotalItemCount(count).RouteData(routeData);

            var model = new AdminIndexViewModel
            {
                Indexes = results,
                Options = options,
                Pager = pagerShape
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Reset"], Value = nameof(ContentsBulkAction.Reset) },
                new SelectListItem() { Text = S["Rebuild"], Value = nameof(ContentsBulkAction.Rebuild) },
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Filter")]
        public ActionResult IndexFilterPOST(AdminIndexViewModel model)
        {
            return RedirectToAction("Index", new RouteValueDictionary {
                { "Options.Search", model.Options.Search }
            });
        }

        public async Task<ActionResult> Edit(string indexName = null)
        {
            var IsCreate = String.IsNullOrWhiteSpace(indexName);
            var settings = new ElasticIndexSettings();

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            if (!IsCreate)
            {
                settings = await _elasticIndexSettingsService.GetSettingsAsync(indexName);

                if (settings == null)
                {
                    return NotFound();
                }
            }

            var model = new ElasticIndexSettingsViewModel
            {
                IsCreate = IsCreate,
                IndexName = IsCreate ? "" : settings.IndexName,
                AnalyzerName = IsCreate ? "standardanalyzer" : settings.AnalyzerName,
                IndexLatest = settings.IndexLatest,
                Culture = settings.Culture,
                Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Select(x => new SelectListItem { Text = x.Name + " (" + x.DisplayName + ")", Value = x.Name }).Prepend(new SelectListItem { Text = S["Any culture"], Value = "any" }),
                Analyzers = _elasticAnalyzerManager.GetAnalyzers()
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Name }),
                IndexedContentTypes = IsCreate ? _contentDefinitionManager.ListTypeDefinitions()
                    .Select(x => x.Name).ToArray() : settings.IndexedContentTypes
            };

            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        public async Task<ActionResult> EditPost(ElasticIndexSettingsViewModel model, string[] indexedContentTypes)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            bool nameWasSplit = false;
            //This was needed to work-around the name validation
            if(!String.IsNullOrEmpty(model.IndexName))
            {
                //Just before validation we remove shellName
                string[] indexNameParts = model.IndexName.Split("_");
                if(indexNameParts.Length >= 1)
                {
                    if(indexNameParts[0].ToLower() == _shellSettings.Name.ToLower())
                    {
                        model.IndexName = indexNameParts[1];
                        nameWasSplit = true;
                    }
                }
            }
            
            ValidateModel(model);

            if (model.IsCreate)
            {
                //We will need to add ShellName here to keep the indexes unique/Scoped
                model.IndexName = $"{_shellSettings.Name}_{model.IndexName}".ToLower();
                
                if (await _elasticIndexManager.Exists(model.IndexName))
                {
                    ModelState.AddModelError(nameof(ElasticIndexSettingsViewModel.IndexName), S["An index named {0} already exists.", model.IndexName]);
                }
            }
            else
            {
                if(nameWasSplit)
                {
                    model.IndexName = $"{_shellSettings.Name}_{model.IndexName}".ToLower();
                }
                if (! await _elasticIndexManager.Exists(model.IndexName))
                {
                    ModelState.AddModelError(nameof(ElasticIndexSettingsViewModel.IndexName), S["An index named {0} doesn't exist.", model.IndexName]);
                }
            }

            if (!ModelState.IsValid)
            {
                model.Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Select(x => new SelectListItem { Text = x.Name + " (" + x.DisplayName + ")", Value = x.Name }).Prepend(new SelectListItem { Text = S["Any culture"], Value = "any" });
                model.Analyzers = _elasticAnalyzerManager.GetAnalyzers()
                    .Select(x => new SelectListItem { Text = x.Name, Value = x.Name });
                return View(model);
            }

            if (model.IsCreate)
            {
                try
                {
                    var settings = new ElasticIndexSettings { IndexName = model.IndexName, AnalyzerName = model.AnalyzerName, IndexLatest = model.IndexLatest, IndexedContentTypes = indexedContentTypes, Culture = model.Culture ?? "" };

                    // We call Rebuild in order to reset the index state cursor too in case the same index
                    // name was also used previously.
                    await _elasticIndexingService.CreateIndexAsync(settings);
                }
                catch (Exception e)
                {
                    _notifier.Error(H["An error occurred while creating the index."]);
                    _logger.LogError(e, "An error occurred while creating an index.");
                    return View(model);
                }

                _notifier.Success(H["Index <em>{0}</em> created successfully.", model.IndexName]);
            }
            else
            {
                try
                {
                    var settings = new ElasticIndexSettings { IndexName = model.IndexName, AnalyzerName = model.AnalyzerName, IndexLatest = model.IndexLatest, IndexedContentTypes = indexedContentTypes, Culture = model.Culture ?? "" };

                    await _elasticIndexingService.UpdateIndexAsync(settings);
                }
                catch (Exception e)
                {
                    _notifier.Error(H["An error occurred while editing the index."]);
                    _logger.LogError(e, "An error occurred while editing an index.");
                    return View(model);
                }

                _notifier.Success(H["Index <em>{0}</em> modified successfully, <strong>please consider doing a rebuild on the index.</strong>", model.IndexName]);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Reset(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            if (!await _elasticIndexManager.Exists(id))
            {
                return NotFound();
            }

            _elasticIndexingService.ResetIndex(id);
            await _elasticIndexingService.ProcessContentItemsAsync(id);

            _notifier.Success(H["Index <em>{0}</em> reset successfully.", id]);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Rebuild(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            if (! await _elasticIndexManager.Exists(id))
            {
                return NotFound();
            }

            await _elasticIndexingService.RebuildIndexAsync(id);
            await _elasticIndexingService.ProcessContentItemsAsync(id);

            _notifier.Success(H["Index <em>{0}</em> rebuilt successfully.", id]);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Delete(ElasticIndexSettingsViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            if (! await _elasticIndexManager.Exists(model.IndexName))
            {
                return NotFound();
            }

            try
            {
                await _elasticIndexingService.DeleteIndexAsync(model.IndexName);

                _notifier.Success(H["Index <em>{0}</em> deleted successfully.", model.IndexName]);
            }
            catch (Exception e)
            {
                _notifier.Error(H["An error occurred while deleting the index."]);
                _logger.LogError("An error occurred while deleting the index " + model.IndexName, e);
            }

            return RedirectToAction("Index");
        }

        public Task<IActionResult> Query(string indexName, string query)
        {
            query = String.IsNullOrWhiteSpace(query) ? "" : System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(query));
            return Query(new AdminQueryViewModel { IndexName = indexName, DecodedQuery = query });
        }

        [HttpPost]
        public async Task<IActionResult> Query(AdminQueryViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            model.Indices = (await _elasticIndexSettingsService.GetSettingsAsync()).Select(x => x.IndexName).ToArray();

            // Can't query if there are no indices
            if (model.Indices.Length == 0)
            {
                return RedirectToAction("Index");
            }

            if (String.IsNullOrEmpty(model.IndexName))
            {
                model.IndexName = model.Indices[0];
            }

            if (! await _elasticIndexManager.Exists(model.IndexName))
            {
                return NotFound();
            }

            if (String.IsNullOrWhiteSpace(model.DecodedQuery))
            {
                return View(model);
            }

            if (String.IsNullOrEmpty(model.Parameters))
            {
                model.Parameters = "{ }";
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var context = new ElasticQueryContext(model.IndexName);
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(model.Parameters);
            var tokenizedContent = await _liquidTemplateManager.RenderStringAsync(model.DecodedQuery, _javaScriptEncoder, parameters.Select(x => new KeyValuePair<string, FluidValue>(x.Key, FluidValue.Create(x.Value, _templateOptions.Value))));
            try
            {
                var parameterizedQuery = JObject.Parse(tokenizedContent);
                var elasticTopDocs = await _queryService.SearchAsync(context, parameterizedQuery);

                if (elasticTopDocs != null)
                {
                    model.Documents = elasticTopDocs.TopDocs;
                    model.Count = elasticTopDocs.Count;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while executing query");
                ModelState.AddModelError(nameof(model.DecodedQuery), S["Invalid query : {0}", e.Message]);
            }

            stopwatch.Stop();
            model.Elapsed = stopwatch.Elapsed;
            return View(model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> IndexPost(ViewModels.ContentOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageIndexes))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                var elasticIndexSettings = await _elasticIndexSettingsService.GetSettingsAsync();
                var checkedContentItems = elasticIndexSettings.Where(x => itemIds.Contains(x.IndexName));
                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in checkedContentItems)
                        {
                            await _elasticIndexingService.DeleteIndexAsync(item.IndexName);
                        }
                        _notifier.Success(H["Indices successfully removed."]);
                        break;
                    case ContentsBulkAction.Reset:
                        foreach (var item in checkedContentItems)
                        {
                            if (! await _elasticIndexManager.Exists(item.IndexName))
                            {
                                return NotFound();
                            }

                            _elasticIndexingService.ResetIndex(item.IndexName);
                            await _elasticIndexingService.ProcessContentItemsAsync(item.IndexName);

                            _notifier.Success(H["Index <em>{0}</em> reset successfully.", item.IndexName]);
                        }
                        break;
                    case ContentsBulkAction.Rebuild:
                        foreach (var item in checkedContentItems)
                        {
                            if (!await _elasticIndexManager.Exists(item.IndexName))
                            {
                                return NotFound();
                            }

                            await _elasticIndexingService.RebuildIndexAsync(item.IndexName);
                            await _elasticIndexingService.ProcessContentItemsAsync(item.IndexName);
                            _notifier.Success(H["Index <em>{0}</em> rebuilt successfully.", item.IndexName]);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction("Index");
        }

        private void ValidateModel(ElasticIndexSettingsViewModel model)
        {
            if (model.IndexedContentTypes == null || model.IndexedContentTypes.Count() < 1)
            {
                ModelState.AddModelError(nameof(ElasticIndexSettingsViewModel.IndexedContentTypes), S["At least one content type selection is required."]);
            }

            if (String.IsNullOrWhiteSpace(model.IndexName))
            {
                ModelState.AddModelError(nameof(ElasticIndexSettingsViewModel.IndexName), S["The index name is required."]);
            }
            else if (model.IndexName.ToSafeName() != model.IndexName)
            {
                ModelState.AddModelError(nameof(ElasticIndexSettingsViewModel.IndexName), S["The index name contains unallowed chars."]);
            }
        }
    }
}
