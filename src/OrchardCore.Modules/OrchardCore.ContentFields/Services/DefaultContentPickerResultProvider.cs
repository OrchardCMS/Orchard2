using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Liquid;
using OrchardCore.Localization;
using YesSql;
using YesSql.Services;

namespace OrchardCore.ContentFields.Services
{
    public class DefaultContentPickerResultProvider : IContentPickerResultProvider
    {
        private readonly ILiquidTemplateManager _templateManager;
        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ISession _session;
        private readonly IStringLocalizer S;

        public DefaultContentPickerResultProvider(IContentManager contentManager, IContentDefinitionManager contentDefinitionManager, ISession session, IStringLocalizer<DefaultContentPickerResultProvider> localizer, ILiquidTemplateManager templateManager)
        {
            _templateManager = templateManager;
            _contentManager = contentManager;
            _contentDefinitionManager = contentDefinitionManager;
            _session = session;
            S = localizer;
        }

        public string Name => "Default";

        public async Task<IEnumerable<ContentPickerResult>> Search(ContentPickerSearchContext searchContext)
        {
            var contentTypes = searchContext.ContentTypes;
            if (searchContext.DisplayAllContentTypes)
            {
                contentTypes = _contentDefinitionManager
                    .ListTypeDefinitions()
                    .Where(x => string.IsNullOrEmpty(x.GetSettings<ContentTypeSettings>().Stereotype))
                    .Select(x => x.Name)
                    .AsEnumerable();
            }

            var query = _session.Query<ContentItem, ContentItemIndex>()
                .With<ContentItemIndex>(x => x.ContentType.IsIn(contentTypes) && x.Latest);

            if (!string.IsNullOrEmpty(searchContext.Query))
            {
                query.With<ContentItemIndex>(x => x.DisplayText.Contains(searchContext.Query) || x.ContentType.Contains(searchContext.Query));
            }

            var contentItems = await query.Take(50).ListAsync();

            var results = new List<ContentPickerResult>();

            foreach (var contentItem in contentItems)
            {
                results.Add(new ContentPickerResult
                {
                    ContentItemId = contentItem.ContentItemId,
                    DisplayText = await GetContentPickerItemDescription(contentItem, searchContext.TitlePattern, contentItem.DisplayText),
                    Description = await GetContentPickerItemDescription(contentItem, searchContext.DescriptionPattern, string.Empty),
                    HasPublished = await _contentManager.HasPublishedVersionAsync(contentItem)
                });
            }

            return results.OrderBy(x => x.DisplayText);
        }

        public async Task<string> GetContentPickerItemDescription(ContentItem contentItem, string pattern, string defaultValue)
        {
            var description = defaultValue;
            if (!string.IsNullOrEmpty(pattern))
            {
                var cultureAspect = await _contentManager.PopulateAspectAsync(contentItem, new CultureAspect());
                using (CultureScope.Create(cultureAspect.Culture))
                {
                    description = await _templateManager.RenderStringAsync(pattern, NullEncoder.Default, contentItem,
                        new Dictionary<string, FluidValue>() { [nameof(ContentItem)] = new ObjectValue(contentItem) });
                }
            }

            return description;
        }
    }
}
