using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Navigation;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;

namespace OrchardCore.Taxonomies.Services
{
    public interface ITaxonomyService
    {
        Task<IEnumerable<ContentItem>> QueryCategorizedItemsAsync(TermPart termPart, PagerSlim pager, bool enableOrdering, bool published);

        JObject FindTaxonomyItem(JObject contentItem, string taxonomyItemId);

        Task InitializeCategorizedItemsOrderAsync(string TaxonomyContentItemId);

        int GetTaxonomyTermOrder(ContentItem categorizedItem, string termContentItemId);

        void RegisterCategorizedItemOrder(ContentItem categorizedItem, string termContentItemId, int orderValue);

        Task SyncTaxonomyFieldProperties(TaxonomyField field);
    }
}
