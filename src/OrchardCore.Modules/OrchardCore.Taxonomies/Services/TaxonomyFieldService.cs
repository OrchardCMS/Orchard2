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

namespace OrchardCore.Taxonomies.Services
{
    public class TaxonomyFieldService : ITaxonomyFieldService
    {
        private readonly ISession _session;
        private readonly IContentManager _contentManager;
        private readonly IServiceProvider _serviceProvider;
        private IContentDefinitionManager _contentDefinitionManager;

        public TaxonomyFieldService(
            ISession session,
            IContentManager contentManager,
            IServiceProvider serviceProvider
        )
        {
            _session = session;
            _contentManager = contentManager;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<ContentItem>> QueryCategorizedItemsAsync(TermPart termPart, bool enableOrdering, PagerSlim pager)
        {
            IEnumerable<ContentItem> containedItems;

            IQuery<ContentItem> query = null;
            if (pager.Before != null)
            {
                if (enableOrdering)
                {
                    var beforeValue = int.Parse(pager.Before);
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId && x.Order > beforeValue)
                        .OrderBy(x => x.Order)
                        .With<ContentItemIndex>(x => x.Published || (termPart.Ordering && x.Latest))
                        .Take(pager.PageSize + 1);
                }
                else
                {
                    var beforeValue = new DateTime(long.Parse(pager.Before));
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId)
                        .With<ContentItemIndex>(x => x.Published && x.CreatedUtc > beforeValue)
                        .OrderBy(x => x.CreatedUtc)
                        .Take(pager.PageSize + 1);
                }

                containedItems = await query.ListAsync();

                if (containedItems.Count() == 0)
                {
                    return containedItems;
                }

                containedItems = containedItems.Reverse();

                // There is always an After as we clicked on Before
                pager.Before = null;
                if (enableOrdering)
                {
                    pager.After = GetTaxonomyTermOrder(containedItems.Last(), termPart.ContentItem.ContentItemId).ToString();
                }
                else
                {
                    pager.After = containedItems.Last().CreatedUtc.Value.Ticks.ToString();
                }

                if (containedItems.Count() == pager.PageSize + 1)
                {
                    containedItems = containedItems.Skip(1);
                    if (enableOrdering)
                    {
                        pager.Before = GetTaxonomyTermOrder(containedItems.First(), termPart.ContentItem.ContentItemId).ToString();
                    }
                    else
                    {
                        pager.Before = containedItems.First().CreatedUtc.Value.Ticks.ToString();
                    }
                }
            }
            else if (pager.After != null)
            {
                if (enableOrdering)
                {
                    var afterValue = int.Parse(pager.After);
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId && x.Order < afterValue)
                        .OrderByDescending(x => x.Order)
                        .With<ContentItemIndex>(x => x.Published || (termPart.Ordering && x.Latest))
                        .Take(pager.PageSize + 1);
                }
                else
                {
                    var afterValue = new DateTime(long.Parse(pager.After));
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId)
                        .With<ContentItemIndex>(x => x.Published && x.CreatedUtc < afterValue)
                        .OrderByDescending(x => x.CreatedUtc)
                        .Take(pager.PageSize + 1);
                }
                containedItems = await query.ListAsync();

                if (containedItems.Count() == 0)
                {
                    return containedItems;
                }

                // There is always a Before page as we clicked on After
                if (enableOrdering)
                {
                    pager.Before = GetTaxonomyTermOrder(containedItems.First(), termPart.ContentItem.ContentItemId).ToString();
                }
                else
                {
                    pager.Before = containedItems.First().CreatedUtc.Value.Ticks.ToString();
                }
                pager.After = null;

                if (containedItems.Count() == pager.PageSize + 1)
                {
                    containedItems = containedItems.Take(pager.PageSize);
                    if (enableOrdering)
                    {
                        pager.After = GetTaxonomyTermOrder(containedItems.Last(), termPart.ContentItem.ContentItemId).ToString();
                    }
                    else
                    {
                        pager.After = containedItems.Last().CreatedUtc.Value.Ticks.ToString();
                    }
                }
            }
            else
            {
                if (enableOrdering)
                {
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId)
                        .OrderByDescending(x => x.Order)
                        .With<ContentItemIndex>(x => x.Published || (termPart.Ordering && x.Latest))
                        .Take(pager.PageSize + 1);
                }
                else
                {
                    query = _session.Query<ContentItem>()
                        .With<TaxonomyIndex>(x => x.TermContentItemId == termPart.ContentItem.ContentItemId)
                        .With<ContentItemIndex>(x => x.Published)
                        .OrderByDescending(x => x.CreatedUtc)
                        .Take(pager.PageSize + 1);
                }

                containedItems = await query.ListAsync();

                if (containedItems.Count() == 0)
                {
                    return containedItems;
                }

                pager.Before = null;
                pager.After = null;

                if (containedItems.Count() == pager.PageSize + 1)
                {
                    containedItems = containedItems.Take(pager.PageSize);
                    if (enableOrdering)
                    {
                        pager.After = GetTaxonomyTermOrder(containedItems.Last(), termPart.ContentItem.ContentItemId).ToString();
                    }
                    else
                    {
                        pager.After = containedItems.Last().CreatedUtc.Value.Ticks.ToString();
                    }
                }
            }

            return (await _contentManager.LoadAsync(containedItems));
        }

        public async Task InitializeCategorizedItemsOrderAsync(string taxonomyContentItemId)
        {
            var taxonomy = await _contentManager.GetAsync(taxonomyContentItemId);
            if (taxonomy == null)
            {
                return;
            }

            foreach (var term in taxonomy.As<TaxonomyPart>().Terms)
            {
                var categorizedItems = await _session.Query<ContentItem>()
                    .With<TaxonomyIndex>(t => t.TermContentItemId == term.ContentItemId)
                    .OrderByDescending(t => t.Order)
                    .With<ContentItemIndex>(c => c.Published || c.Latest)
                    .ThenByDescending(c => c.CreatedUtc)
                    .ListAsync();

                var startingOrder = categorizedItems.Count();

                SaveCategorizedItemsOrder(categorizedItems, term.ContentItemId, startingOrder);
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
                    // When categorized with a new term, if ordering is enabled, the content item goes into the first (higher order) position.
                    field.TermContentItemOrder.Add(term, await GetNextOrderNumberAsync(term));
                }
            }

            // Remove any content or the elements would be merged (removed elements would not be cleared), because JsonMerge.ArrayHandling.Replace doesn't handle dictionaries.
            field.Content.TermContentItemOrder?.RemoveAll();
        }

        public void SaveCategorizedItemsOrder(IEnumerable<ContentItem> categorizedItems, string termContentItemId, int topOrderValue)
        {
            var orderValue = topOrderValue;

            // The list of content items is already ordered (first to last), all we do here is register that order on the appropriate field for each content item, starting with topOrderValue and descending from there.
            foreach (var categorizedItem in categorizedItems)
            {
                RegisterCategorizedItemOrder(categorizedItem, termContentItemId, orderValue);
                --orderValue;
            }

            return;
        }

        public int GetTaxonomyTermOrder(ContentItem categorizedItem, string termContentItemId)
        {
            (var field, var fieldDefinition) = GetTaxonomyFielForTerm(categorizedItem: categorizedItem, termContentItemId: termContentItemId);
            return field.TermContentItemOrder[termContentItemId];
        }

        private void RegisterCategorizedItemOrder(ContentItem categorizedItem, string termContentItemId, int orderValue)
        {
            (var field, var fieldDefinition) = GetTaxonomyFielForTerm(categorizedItem: categorizedItem, termContentItemId: termContentItemId);

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

        private async Task<int> GetNextOrderNumberAsync(string termContentItemId)
        {
            var indexes = await _session.QueryIndex<TaxonomyIndex>(t => t.TermContentItemId == termContentItemId).ListAsync();
            if (indexes.Any())
            {
                return indexes.Max(t => t.Order) + 1;
            }

            return 1;
        }

        // Given a content item, this method returns the taxonomy field for a specific taxonomy and/or the one that includes (categorizes) the content item in a specific taxonomy term.
        private (TaxonomyField field, ContentPartFieldDefinition fieldDefinition) GetTaxonomyFielForTerm(ContentItem categorizedItem, string taxonomyContentItemId = null, string termContentItemId = null)
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

                if ((taxonomyContentItemId == null || field.TaxonomyContentItemId == taxonomyContentItemId) && (termContentItemId == null || field.TermContentItemIds.Contains(termContentItemId)))
                {
                    return (field, fieldDefinition);
                }
            }

            return (null, null);
        }
    }
}
