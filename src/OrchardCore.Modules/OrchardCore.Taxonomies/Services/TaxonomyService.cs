using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Navigation;
using OrchardCore.Taxonomies.Indexing;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using YesSql;
using OrchardCore.Mvc.Utilities;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement.Metadata.Models;
using System.Diagnostics;

namespace OrchardCore.Taxonomies.Services
{
    public class TaxonomyService : ITaxonomyService
    {
        private readonly ISession _session;
        private readonly IContentManager _contentManager;
        private readonly IServiceProvider _serviceProvider;
        private IContentDefinitionManager _contentDefinitionManager;

        public TaxonomyService(
            ISession session,
            IContentManager contentManager,
            IServiceProvider serviceProvider
        )
        {
            _session = session;
            _contentManager = contentManager;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<ContentItem>> QueryCategorizedItemsAsync(TermPart termPart, PagerSlim pager, bool enableOrdering, bool published)
        {
            // Prepare the query
            var query = _session.Query<ContentItem>()
                .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId);

            if (!enableOrdering)
            // Manual ordering is not enabled, so we order by CreatedUtc value
            {
                if (pager.Before != null)
                {
                    var beforeValue = new DateTime(long.Parse(pager.Before));
                    query = query = query
                        .Where(x => x.CreatedUtc > beforeValue)
                        .OrderBy(x => x.CreatedUtc);
                }
                else
                {
                    if (pager.After != null)
                    {
                        var afterValue = new DateTime(long.Parse(pager.After));
                        query = query = query.Where(x => x.CreatedUtc < afterValue);
                    }

                    query = query.OrderByDescending(x => x.CreatedUtc);
                }
            }
            else
            // Manual ordering is enabled, so we order by Order value
            {
                if (pager.Before != null)
                {
                    var beforeValue = int.Parse(pager.Before);
                    query = query
                        .Where(x => x.Order > beforeValue)
                        .OrderBy(x => x.Order);
                }
                else
                {
                    if (pager.After != null)
                    {
                        var afterValue = int.Parse(pager.After);
                        query = query = query.Where(x => x.Order < afterValue);
                    }

                    query = query.OrderByDescending(x => x.Order);
                }
            }

            if (published)
            {
                query = query.Where(x => x.Published);
            }
            else
            {
                query = query.Where(x => x.Latest);
            }

            // Get the categorized items
            var categorizedItems = await query.Take(pager.PageSize + 1).ListAsync();

            if (categorizedItems.Count() == 0)
            {
                return categorizedItems;
            }

            if (pager.Before != null)
            {
                categorizedItems = categorizedItems.Reverse();

                // There is always an After as we clicked on Before
                pager.Before = null;

                pager.After = enableOrdering ?
                    GetTaxonomyTermOrder(categorizedItems.Last(), termPart.ContentItem.ContentItemId).ToString() :
                    categorizedItems.Last().CreatedUtc.Value.Ticks.ToString();

                if (categorizedItems.Count() == pager.PageSize + 1)
                {
                    categorizedItems = categorizedItems.Skip(1);
                    pager.Before = enableOrdering ?
                        GetTaxonomyTermOrder(categorizedItems.First(), termPart.ContentItem.ContentItemId).ToString() :
                        pager.Before = categorizedItems.First().CreatedUtc.Value.Ticks.ToString();
                }
            }
            else if (pager.After != null)
            {
                // There is always a Before page as we clicked on After
                pager.Before = enableOrdering ?
                    GetTaxonomyTermOrder(categorizedItems.First(), termPart.ContentItem.ContentItemId).ToString() :
                    pager.Before = categorizedItems.First().CreatedUtc.Value.Ticks.ToString();

                pager.After = null;

                if (categorizedItems.Count() == pager.PageSize + 1)
                {
                    categorizedItems = categorizedItems.Take(pager.PageSize);
                    pager.After = enableOrdering ?
                        GetTaxonomyTermOrder(categorizedItems.Last(), termPart.ContentItem.ContentItemId).ToString() :
                        pager.After = categorizedItems.Last().CreatedUtc.Value.Ticks.ToString();
                }
            }
            else
            {
                pager.Before = null;
                pager.After = null;

                if (categorizedItems.Count() == pager.PageSize + 1)
                {
                    if (pager.PageSize > 0)
                    {
                        categorizedItems = categorizedItems.Take(pager.PageSize);
                    }
                    pager.After = enableOrdering ?
                        GetTaxonomyTermOrder(categorizedItems.Last(), termPart.ContentItem.ContentItemId).ToString() :
                        pager.After = categorizedItems.Last().CreatedUtc.Value.Ticks.ToString();
                }
            }

            return categorizedItems;
        }

        public JObject FindTaxonomyItem(JObject contentItem, string taxonomyItemId)
        {
            if (contentItem["ContentItemId"]?.Value<string>() == taxonomyItemId)
            {
                return contentItem;
            }

            if (contentItem.GetValue("Terms") == null)
            {
                return null;
            }

            var taxonomyItems = (JArray)contentItem["Terms"];

            JObject result;

            foreach (JObject taxonomyItem in taxonomyItems)
            {
                // Search in inner taxonomy items
                result = FindTaxonomyItem(taxonomyItem, taxonomyItemId);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public async Task InitializeCategorizedItemsOrderAsync(string taxonomyContentItemId)
        {
            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId);
            if (taxonomy == null)
            {
                return;
            }

            var terms = new List<ContentItem>();
            GetAllTerms(taxonomy.Content.TaxonomyPart.Terms as JArray, terms);
            foreach (var term in terms)
            {
                var categorizedItems = await _session.Query<ContentItem>()
                    .With<TaxonomyIndex>(t => t.TermContentItemId == term.ContentItemId)
                    .OrderByDescending(t => t.Order)
                    .ThenByDescending(t => t.CreatedUtc)
                    .ListAsync();

                var categorizedItemIds = categorizedItems.Select(x => x.ContentItemId).Distinct().ToArray();

                var orderValue = categorizedItemIds.Length;
                foreach (var categorizedItemId in categorizedItemIds)
                {
                    // All the versions (Published and Latest) of each content item get the same Order value
                    foreach (var categorizedItem in categorizedItems.Where(c => c.ContentItemId == categorizedItemId))
                    {
                        RegisterCategorizedItemOrder(categorizedItem, term.ContentItemId, orderValue);
                    }
                    orderValue--;
                }
            }
        }
        private void GetAllTerms(JArray termsArray, List<ContentItem> terms)
        {
            foreach (JObject term in termsArray)
            {
                terms.Add(term.ToObject<ContentItem>());
                if (term.GetValue("Terms") is JArray children)
                {
                    GetAllTerms(children, terms);
                }
            }
        }

        public int GetTaxonomyTermOrder(ContentItem categorizedItem, string termContentItemId)
        {
            (var field, var fieldDefinition) = GetTaxonomyField(categorizedItem, termContentItemId);
            return field.TermContentItemOrder[termContentItemId];
        }

        public void RegisterCategorizedItemOrder(ContentItem categorizedItem, string termContentItemId, int orderValue)
        {
            (var field, var fieldDefinition) = GetTaxonomyField(categorizedItem, termContentItemId);

            if (field != null)
            {
                var currentOrder = field.TermContentItemOrder.GetValueOrDefault(termContentItemId, 0);

                if (orderValue != currentOrder)
                {
                    field.TermContentItemOrder[termContentItemId] = orderValue;

                    var jPart = (JObject)categorizedItem.Content[fieldDefinition.PartDefinition.Name];
                    jPart[fieldDefinition.Name] = JObject.FromObject(field);
                    categorizedItem.Content[fieldDefinition.PartDefinition.Name] = jPart;

                    _session.Save(categorizedItem);
                }
            }
        }

        // Add or remove from TermContentItemOrder elements that were added or removed from TermContentItemIds.
        public async Task SyncTaxonomyFieldProperties(TaxonomyField field)
        {
            var removedTerms = field.TermContentItemOrder.Where(o => !field.TermContentItemIds.Contains(o.Key)).Select(o => o.Key).ToList();
            foreach (var removedTerm in removedTerms)
            {
                // Remove the order information because the content item in no longer categorized with this term.
                field.TermContentItemOrder.Remove(removedTerm);
            }

            foreach (var term in field.TermContentItemIds)
            {
                if (!field.TermContentItemOrder.ContainsKey(term))
                {
                    // When categorized with a new term, the content item goes into the first (higher order) position.
                    field.TermContentItemOrder.Add(term, await GetNextOrderNumberAsync(term));
                }
            }

            // Remove any content or the elements would be merged (removed elements would not be cleared), because JsonMerge.ArrayHandling.Replace doesn't handle dictionaries.
            field.Content.TermContentItemOrder?.RemoveAll();
        }

        private async Task<int> GetNextOrderNumberAsync(string termContentItemId)
        {
            var indexes = await _session.QueryIndex<TaxonomyIndex>(t => t.TermContentItemId == termContentItemId).ListAsync();
            if (indexes.Any())
            {
                return indexes.Max(t => t.Order) + 1;
            }

            return 1;
        }

        private (TaxonomyField field, ContentPartFieldDefinition fieldDefinition) GetTaxonomyField(ContentItem categorizedItem, string termContentItemId)
        {
            _contentDefinitionManager = _contentDefinitionManager ?? _serviceProvider.GetRequiredService<IContentDefinitionManager>();

            var fieldDefinitions = _contentDefinitionManager
                    .GetTypeDefinition(categorizedItem.ContentType)
                    .Parts.SelectMany(x => x.PartDefinition.Fields.Where(f => f.FieldDefinition.Name == nameof(TaxonomyField)))
                    .ToArray();

            foreach (var fieldDefinition in fieldDefinitions)
            {
                var jPart = (JObject)categorizedItem.Content[fieldDefinition.PartDefinition.Name];
                if (jPart == null)
                {
                    continue;
                }

                var jField = (JObject)jPart[fieldDefinition.Name];
                if (jField == null)
                {
                    continue;
                }

                var field = jField.ToObject<TaxonomyField>();

                if (field.TermContentItemIds.Contains(termContentItemId))
                {
                    return (field, fieldDefinition);
                }
            }

            return (null, null);
        }
    }
}
