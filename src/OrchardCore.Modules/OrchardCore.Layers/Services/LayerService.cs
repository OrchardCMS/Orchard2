using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data;
using OrchardCore.Environment.Cache;
using OrchardCore.Layers.Indexes;
using OrchardCore.Layers.Models;
using YesSql;

namespace OrchardCore.Layers.Services
{
    public class LayerService : ILayerService
    {
        private const string LayersCacheKey = "LayersDocument";

        private readonly ISignal _signal;
        private readonly ISession _session;
        private readonly ISessionHelper _sessionHelper;
        private readonly IMemoryCache _memoryCache;
        private readonly IOrchardHelper _orchardHelper;

        public LayerService(
            ISignal signal,
            ISession session,
            ISessionHelper sessionHelper,
            IMemoryCache memoryCache,
            IOrchardHelper orchardHelper)
        {
            _signal = signal;
            _session = session;
            _sessionHelper = sessionHelper;
            _memoryCache = memoryCache;
            _orchardHelper = orchardHelper;
        }

        public IChangeToken ChangeToken => _signal.GetToken(LayersCacheKey);

        /// <summary>
        /// Returns the document from the database to be updated.
        /// </summary>
        public Task<LayersDocument> LoadLayersAsync() => _sessionHelper.LoadForUpdateAsync<LayersDocument>();

        /// <summary>
        /// Returns the document from the cache or creates a new one. The result should not be updated.
        /// </summary>
        public async Task<LayersDocument> GetLayersAsync()
        {
            if (!_memoryCache.TryGetValue<LayersDocument>(LayersCacheKey, out var layers))
            {
                var changeToken = ChangeToken;

                layers = await _sessionHelper.GetForCachingAsync<LayersDocument>();

                layers.IsReadonly = true;

                _memoryCache.Set(LayersCacheKey, layers, changeToken);
            }

            return layers;
        }

        public async Task<IEnumerable<ContentItem>> GetLayerWidgetsAsync(
            Expression<Func<ContentItemIndex, bool>> predicate)
        {
            return await _session
                .Query<ContentItem, LayerMetadataIndex>()
                .With(predicate)
                .ListAsync();
        }

        public async Task<IEnumerable<LayerMetadata>> GetLayerWidgetsMetadataAsync(
            Expression<Func<ContentItemIndex, bool>> predicate)
        {
            var allWidgets = await GetLayerWidgetsAsync(predicate);

            return allWidgets
                .Select(x => x.As<LayerMetadata>())
                .Where(x => x != null)
                .OrderBy(x => x.Position)
                .ToList();
        }
        
        public async IAsyncEnumerable<LayerMetadata> FilterWidgetsByCultureAsync(IEnumerable<LayerMetadata> widgets, string culture)
        {
            foreach (var widget in widgets)
            {
                var cultureInfo = await _orchardHelper.GetContentCultureAsync(widget.ContentItem);
                if (cultureInfo.Name == culture)
                    yield return widget;
            }
        }

        public async Task UpdateAsync(LayersDocument layers)
        {
            if (layers.IsReadonly)
            {
                throw new ArgumentException("The object is read-only");
            }

            var existing = await LoadLayersAsync();
            existing.Layers = layers.Layers;

            _session.Save(existing);
            _signal.DeferredSignalToken(LayersCacheKey);
        }
    }
}
