using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Layers.Models;

namespace OrchardCore.Layers.Services
{
    public interface ILayerService
    {
        /// <summary>
        /// Returns all the layers for update.
        /// </summary>
        Task<LayersDocument> LoadLayersAsync();

        /// <summary>
        /// Returns all the layers in read-only.
        /// </summary>
        Task<LayersDocument> GetLayersAsync();

        Task<IEnumerable<ContentItem>> GetLayerWidgetsAsync(Expression<Func<ContentItemIndex, bool>> predicate);
        Task<IEnumerable<LayerMetadata>> GetLayerWidgetsMetadataAsync(Expression<Func<ContentItemIndex, bool>> predicate);
        Task UpdateAsync(LayersDocument layers);

        /// <summary>
        /// Returns widgets that match the specified culture.
        /// </summary>
        /// <param name="widgets">The sequence of widgets to filter.</param>
        /// <param name="culture">Culture being applied as a filter.</param>
        IAsyncEnumerable<LayerMetadata> FilterWidgetsByCultureAsync(IEnumerable<LayerMetadata> widgets, string culture);

        /// <summary>
        /// Gets a change token that is set when the layers have changed.
        /// </summary>
        IChangeToken ChangeToken { get; }
    }
}
