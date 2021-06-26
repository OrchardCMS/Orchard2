using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Search.Elastic.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.Search.Elastic.Drivers
{
    public class ElasticSettingsDisplayDriver : SectionDisplayDriver<ISite, ElasticSettings>
    {
        public const string GroupId = "search";
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public ElasticSettingsDisplayDriver(
            ElasticIndexSettingsService elasticIndexSettingsService,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService
            )
        {
            _elasticIndexSettingsService = elasticIndexSettingsService;
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(ElasticSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageIndexes))
            {
                return null;
            }

            return Initialize<ElasticSettingsViewModel>("ElasticSettings_Edit", async model =>
                {
                    model.SearchIndex = settings.SearchIndex;
                    model.SearchFields = String.Join(", ", settings.DefaultSearchFields ?? new string[0]);
                    model.SearchIndexes = (await _elasticIndexSettingsService.GetSettingsAsync()).Select(x => x.IndexName);
                    model.AllowElasticQueriesInSearch = settings.AllowElasticQueriesInSearch;
                }).Location("Content:2").OnGroup(GroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(ElasticSettings section, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageIndexes))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var model = new ElasticSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                section.SearchIndex = model.SearchIndex;
                section.DefaultSearchFields = model.SearchFields?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                section.AllowElasticQueriesInSearch = model.AllowElasticQueriesInSearch;
            }

            return await EditAsync(section, context);
        }
    }
}
