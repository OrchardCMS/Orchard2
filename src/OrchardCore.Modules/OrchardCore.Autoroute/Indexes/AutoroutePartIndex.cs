using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Routing;
using OrchardCore.Data;
using YesSql.Indexes;

namespace OrchardCore.ContentManagement.Records
{
    public class AutoroutePartIndex : MapIndex
    {
        /// <summary>
        /// The id of the document.
        /// </summary>
        public int DocumentId { get; set; }

        /// <summary>
        /// The container content item id.
        /// </summary>
        public string ContentItemId { get; set; }

        /// <summary>
        /// Route path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether this content item is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Whether this content item is latest.
        /// </summary>
        public bool Latest { get; set; }

        /// <summary>
        /// Only used if content item is contained in a container.
        /// </summary>
        public string ContainedContentItemId { get; set; }

        /// <summary>
        /// Only used if the content item is contained in a container.
        /// </summary>
        public string JsonPath { get; set; }
    }

    public class AutoroutePartIndexProvider : ContentHandlerBase, IIndexProvider, IScopedIndexProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HashSet<ContentItem> _itemRemoved = new HashSet<ContentItem>();
        private readonly HashSet<string> _partRemoved = new HashSet<string>();
        private IContentDefinitionManager _contentDefinitionManager;
        private IContentManager _contentManager;

        public AutoroutePartIndexProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override Task RemovedAsync(RemoveContentContext context)
        {
            if (context.NoActiveVersionLeft)
            {
                var part = context.ContentItem.As<AutoroutePart>();

                if (part != null)
                {
                    _itemRemoved.Add(context.ContentItem);
                }
            }

            return Task.CompletedTask;
        }

        public override async Task PublishedAsync(PublishContentContext context)
        {
            var part = context.ContentItem.As<AutoroutePart>();

            // Validate that the content definition contains this part, this prevents indexing parts
            // that have been removed from the type definition, but are still present in the elements.            
            if (part != null)
            {
                // Lazy initialization because of ISession cyclic dependency.
                _contentDefinitionManager ??= _serviceProvider.GetRequiredService<IContentDefinitionManager>();

                // Search for this part.
                var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
                if (!contentTypeDefinition.Parts.Any(ctpd => ctpd.Name == nameof(AutoroutePart)))
                {
                    context.ContentItem.Remove<AutoroutePart>();
                    _partRemoved.Add(context.ContentItem.ContentItemId);

                    // When the part has been removed enlist an update for after the session has been committed.
                    var autorouteEntries = _serviceProvider.GetRequiredService<IAutorouteEntries>();
                    await autorouteEntries.UpdateEntriesAsync();
                }
            }
        }

        public string CollectionName { get; set; }
        public Type ForType() => typeof(ContentItem);
        public void Describe(IDescriptor context) => Describe((DescribeContext<ContentItem>)context);

        public void Describe(DescribeContext<ContentItem> context)
        {
            context.For<AutoroutePartIndex>()
                .When(contentItem => contentItem.Has<AutoroutePart>() || _partRemoved.Contains(contentItem.ContentItemId))
                .Map(async contentItem =>
                {
                    // If the content item was removed, a record is still added.
                    var itemRemoved = _itemRemoved.Contains(contentItem);
                    if (!contentItem.Published && !contentItem.Latest && !itemRemoved)
                    {
                        return null;
                    }

                    // If the part was removed from the type definition, a record is still added.
                    var partRemoved = _partRemoved.Contains(contentItem.ContentItemId);

                    var part = contentItem.As<AutoroutePart>();
                    if (!partRemoved && (part == null || String.IsNullOrEmpty(part.Path)))
                    {
                        return null;
                    }

                    var results = new List<AutoroutePartIndex>
                    {
                        // If the part is disabled or was removed, a record is still added but with a null path.
                        new AutoroutePartIndex
                        {
                            ContentItemId = contentItem.ContentItemId,
                            Path = !partRemoved && !part.Disabled ? part.Path : null,
                            Published = contentItem.Published,
                            Latest = contentItem.Latest
                        }
                    };

                    if (partRemoved || !part.RouteContainedItems || part.Disabled || itemRemoved)
                    {
                        return results;
                    }

                    _contentManager ??= _serviceProvider.GetRequiredService<IContentManager>();

                    var containedContentItemsAspect = await _contentManager.PopulateAspectAsync<ContainedContentItemsAspect>(contentItem);

                    await PopulateContainedContentItemIndexesAsync(results, contentItem, containedContentItemsAspect, contentItem.Content, part.Path);

                    return results;
                });
        }

        private async Task PopulateContainedContentItemIndexesAsync(List<AutoroutePartIndex> results, ContentItem containerContentItem, ContainedContentItemsAspect containedContentItemsAspect, JObject content, string basePath)
        {
            foreach (var accessor in containedContentItemsAspect.Accessors)
            {
                var items = accessor.Invoke(content);

                foreach (JObject jItem in items)
                {
                    var contentItem = jItem.ToObject<ContentItem>();
                    var handlerAspect = await _contentManager.PopulateAspectAsync<RouteHandlerAspect>(contentItem);

                    if (!handlerAspect.Disabled)
                    {
                        var path = handlerAspect.Path;
                        if (!handlerAspect.Absolute)
                        {
                            path = (basePath.EndsWith('/') ? basePath : basePath + '/') + handlerAspect.Path.TrimStart('/');
                        }

                        results.Add(new AutoroutePartIndex
                        {
                            ContentItemId = containerContentItem.ContentItemId,
                            Path = path,
                            Published = containerContentItem.Published,
                            Latest = containerContentItem.Latest,
                            ContainedContentItemId = contentItem.ContentItemId,
                            JsonPath = jItem.Path
                        });
                    }

                    var itemBasePath = (basePath.EndsWith('/') ? basePath : basePath + '/') + handlerAspect.Path.TrimStart('/');
                    var childrenAspect = await _contentManager.PopulateAspectAsync<ContainedContentItemsAspect>(contentItem);

                    await PopulateContainedContentItemIndexesAsync(results, containerContentItem, childrenAspect, jItem, itemBasePath);
                }
            }
        }
    }
}
