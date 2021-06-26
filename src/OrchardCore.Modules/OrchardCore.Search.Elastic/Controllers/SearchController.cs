using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement;
using OrchardCore.Entities;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Search.Elastic.Services;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Search.Abstractions.ViewModels;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Search.Elastic.Controllers
{
    public class SearchController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ISiteService _siteService;
        private readonly ElasticIndexManager _elasticIndexProvider;
        private readonly ElasticIndexingService _elasticIndexingService;
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;
        private readonly ElasticAnalyzerManager _elasticAnalyzerManager;
        private readonly ISearchQueryService _searchQueryService;
        private readonly ISession _session;
        private readonly IStringLocalizer S;
        private readonly IEnumerable<IPermissionProvider> _permissionProviders;
        private readonly dynamic New;
        private readonly ILogger _logger;

        public SearchController(
            IAuthorizationService authorizationService,
            ISiteService siteService,
            ElasticIndexManager elasticIndexProvider,
            ElasticIndexingService elasticIndexingService,
            ElasticIndexSettingsService elasticIndexSettingsService,
            ElasticAnalyzerManager elasticAnalyzerManager,
            ISearchQueryService searchQueryService,
            ISession session,
            IStringLocalizer<SearchController> stringLocalizer,
            IEnumerable<IPermissionProvider> permissionProviders,
            IShapeFactory shapeFactory,
            ILogger<SearchController> logger
            )
        {
            _authorizationService = authorizationService;
            _siteService = siteService;
            _elasticIndexProvider = elasticIndexProvider;
            _elasticIndexingService = elasticIndexingService;
            _elasticIndexSettingsService = elasticIndexSettingsService;
            _elasticAnalyzerManager = elasticAnalyzerManager;
            _searchQueryService = searchQueryService;
            _session = session;
            S = stringLocalizer;
            _permissionProviders = permissionProviders;
            New = shapeFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search(SearchIndexViewModel viewModel, PagerSlimParameters pagerParameters)
        {
            var permissionsProvider = _permissionProviders.FirstOrDefault(x => x.GetType().FullName == "OrchardCore.Search.Elastic.Permissions");
            var permissions = await permissionsProvider.GetPermissionsAsync();

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var searchSettings = siteSettings.As<ElasticSettings>();

            if (permissions.FirstOrDefault(x => x.Name == "QueryElastic" + searchSettings.SearchIndex + "Index") != null)
            {
                if (!await _authorizationService.AuthorizeAsync(User, permissions.FirstOrDefault(x => x.Name == "QueryElastic" + searchSettings.SearchIndex + "Index")))
                {
                    return this.ChallengeOrForbid();
                }
            }
            else
            {
                _logger.LogInformation("Couldn't execute Elastic search. The search index doesn't exist.");
                return BadRequest("Elastic Search is not configured.");
            }

            if (searchSettings.SearchIndex != null && ! await _elasticIndexProvider.Exists(searchSettings.SearchIndex))
            {
                _logger.LogInformation("Couldn't execute Elastic search. The Elastic search index doesn't exist.");
                return BadRequest("Elastic Search is not configured.");
            }

            var elasticSettings = await _elasticIndexingService.GetElasticSettingsAsync();

            if (elasticSettings == null || elasticSettings?.DefaultSearchFields == null)
            {
                _logger.LogInformation("Couldn't execute Elastic search. No Elastic settings was defined.");
                return BadRequest("Elastic Search is not configured.");
            }

            var elasticIndexSettings = await _elasticIndexSettingsService.GetSettingsAsync(searchSettings.SearchIndex);

            if (elasticIndexSettings == null)
            {
                _logger.LogInformation($"Couldn't execute search. No Elastic index settings was defined for ({searchSettings.SearchIndex}) index.");
                return BadRequest($"Search index ({searchSettings.SearchIndex}) is not configured.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.Terms))
            {
                return View(new SearchIndexViewModel
                {
                    SearchForm = new SearchFormViewModel("Search__Form") { },
                });
            }

            var pager = new PagerSlim(pagerParameters, siteSettings.PageSize);

            // We Query Elastic index
            var analyzer = _elasticAnalyzerManager.CreateAnalyzer(await _elasticIndexSettingsService.GetIndexAnalyzerAsync(elasticIndexSettings.IndexName));

            // Fetch one more result than PageSize to generate "More" links
            var start = 0;
            var end = pager.PageSize + 1;

            if (pagerParameters.Before != null)
            {
                start = Convert.ToInt32(pagerParameters.Before) - pager.PageSize - 1;
                end = Convert.ToInt32(pagerParameters.Before);
            }
            else if (pagerParameters.After != null)
            {
                start = Convert.ToInt32(pagerParameters.After);
                end = Convert.ToInt32(pagerParameters.After) + pager.PageSize + 1;
            }

            var terms = viewModel.Terms;
            if (!searchSettings.AllowElasticQueriesInSearch)
            {
                //Need to revisit this
                //terms = QueryParser.Escape(terms);
            }

            IList<string> contentItemIds;
            try
            {
                //var query = queryParser.Parse(terms);
                contentItemIds = (await _searchQueryService.ExecuteQueryAsync(viewModel.Terms, searchSettings.SearchIndex, start, end))
                    .ToList();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Terms", S["Incorrect query syntax."]);
                _logger.LogError(e, "Incorrect Elastic search query syntax provided in search:");

                // Return a SearchIndexViewModel without SearchResults or Pager shapes since there is an error.
                return View(new SearchIndexViewModel
                {
                    Terms = viewModel.Terms,
                    SearchForm = new SearchFormViewModel("Search__Form") { Terms = viewModel.Terms },
                });
            }

            // We Query database to retrieve content items.
            IQuery<ContentItem> queryDb;

            if (elasticIndexSettings.IndexLatest)
            {
                queryDb = _session.Query<ContentItem, ContentItemIndex>()
                    .Where(x => x.ContentItemId.IsIn(contentItemIds) && x.Latest == true)
                    .Take(pager.PageSize + 1);
            }
            else
            {
                queryDb = _session.Query<ContentItem, ContentItemIndex>()
                    .Where(x => x.ContentItemId.IsIn(contentItemIds) && x.Published == true)
                    .Take(pager.PageSize + 1);
            }

            // Sort the content items by their rank in the search results returned by Elastic.
            var containedItems = (await queryDb.ListAsync()).OrderBy(x => contentItemIds.IndexOf(x.ContentItemId));

            // We set the PagerSlim before and after links
            if (pagerParameters.After != null || pagerParameters.Before != null)
            {
                if (start + 1 > 1)
                {
                    pager.Before = (start + 1).ToString();
                }
                else
                {
                    pager.Before = null;
                }
            }

            if (containedItems.Count() == pager.PageSize + 1)
            {
                pager.After = (end - 1).ToString();
            }
            else
            {
                pager.After = null;
            }

            var model = new SearchIndexViewModel
            {
                Terms = viewModel.Terms,
                SearchForm = new SearchFormViewModel("Search__Form") { Terms = viewModel.Terms },
                SearchResults = new SearchResultsViewModel("Search__Results") { ContentItems = containedItems.Take(pager.PageSize) },
                Pager = (await New.PagerSlim(pager)).UrlParams(new Dictionary<string, string>() { { "Terms", viewModel.Terms } })
            };

            return View(model);
        }
    }
}
