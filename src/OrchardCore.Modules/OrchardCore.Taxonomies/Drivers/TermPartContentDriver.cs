using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using OrchardCore.Taxonomies.Indexing;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Services;
using OrchardCore.Taxonomies.ViewModels;
using YesSql;

namespace OrchardCore.Taxonomies.Drivers
{
    public class TermPartContentDriver : ContentDisplayDriver
    {
        private readonly ISession _session;
        private readonly ISiteService _siteService;
        private readonly IContentManager _contentManager;
        private readonly ITaxonomyService _taxonomyService;

        public TermPartContentDriver(
            ISession session,
            ISiteService siteService,
            IContentManager contentManager,
            ITaxonomyService taxonomyService)
        {
            _session = session;
            _siteService = siteService;
            _contentManager = contentManager;
            _taxonomyService = taxonomyService;
        }

        public override Task<IDisplayResult> DisplayAsync(ContentItem model, BuildDisplayContext context)
        {
            var part = model.As<TermPart>();
            if (part != null)
            {
                return Task.FromResult<IDisplayResult>(Initialize<TermPartViewModel>("TermPart", async m =>
                {
                    var enableOrdering = (await _contentManager.GetAsync(part.TaxonomyContentItemId, VersionOptions.Latest)).As<TaxonomyPart>().EnableOrdering;
                    var siteSettings = await _siteService.GetSiteSettingsAsync();
                    var pagerParameters = new PagerSlimParameters();
                    await context.Updater.TryUpdateModelAsync(pagerParameters);
                    var pager = new PagerSlim(pagerParameters, siteSettings.PageSize);
                    m.TaxonomyContentItemId = part.TaxonomyContentItemId;
                    m.ContentItem = part.ContentItem;
                    //m.ContentItems = (await QueryTermItemsAsync(part, pager)).ToArray();
                    m.ContentItems = (await _taxonomyService.QueryCategorizedItemsAsync(part, pager, enableOrdering, true)).ToArray();
                    m.Pager = await context.New.PagerSlim(pager);
                    m.EnableOrdering = enableOrdering;
                })
                .Location("Detail", "Content:5"));
            }

            return Task.FromResult<IDisplayResult>(null);
        }
    }
}
