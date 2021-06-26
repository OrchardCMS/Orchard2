using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Environment.Shell;
using OrchardCore.Indexing;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Modules;
using OrchardCore.Settings;

namespace OrchardCore.Search.Elastic
{
    /// <summary>
    /// This class provides services to update all the Elastic indices. It is non-rentrant so that calls
    /// from different components can be done simultaneously, e.g. from a background task, an event or a UI interaction.
    /// It also indexes one content item at a time and provides the result to all indices.
    /// </summary>
    public class ElasticIndexingService
    {
        private const int BatchSize = 100;
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly ElasticIndexingState _indexingState;
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;
        private readonly ElasticIndexManager _indexManager;
        private readonly IIndexingTaskManager _indexingTaskManager;
        private readonly ISiteService _siteService;
        private readonly ILogger _logger;

        public ElasticIndexingService(
            IShellHost shellHost,
            ShellSettings shellSettings,
            ElasticIndexingState indexingState,
            ElasticIndexSettingsService elasticIndexSettingsService,
            ElasticIndexManager indexManager,
            IIndexingTaskManager indexingTaskManager,
            ISiteService siteService,
            ILogger<ElasticIndexingService> logger)
        {
            _shellHost = shellHost;
            _shellSettings = shellSettings;
            _indexingState = indexingState;
            _elasticIndexSettingsService = elasticIndexSettingsService;
            _indexManager = indexManager;
            _indexingTaskManager = indexingTaskManager;
            _siteService = siteService;
            _logger = logger;
        }

        public async Task ProcessContentItemsAsync(string indexName = default)
        {
            // TODO: Lock over the filesystem in case two instances get a command to rebuild the index concurrently.
            var allIndices = new Dictionary<string, int>();
            var lastTaskId = Int32.MaxValue;
            IEnumerable<ElasticIndexSettings> indexSettingsList = null;

            if (String.IsNullOrEmpty(indexName))
            {
                indexSettingsList = await _elasticIndexSettingsService.GetSettingsAsync();

                if (!indexSettingsList.Any())
                {
                    return;
                }

                // Find the lowest task id to process
                foreach (var indexSetting in indexSettingsList)
                {
                    var taskId = _indexingState.GetLastTaskId(indexSetting.IndexName);
                    lastTaskId = Math.Min(lastTaskId, taskId);
                    allIndices.Add(indexSetting.IndexName, taskId);
                }
            }
            else
            {
                var settings = await _elasticIndexSettingsService.GetSettingsAsync(indexName);

                if (settings == null)
                {
                    return;
                }

                indexSettingsList = new ElasticIndexSettings[1] { settings }.AsEnumerable();

                var taskId = _indexingState.GetLastTaskId(indexName);
                lastTaskId = Math.Min(lastTaskId, taskId);
                allIndices.Add(indexName, taskId);
            }

            if (allIndices.Count == 0)
            {
                return;
            }

            var batch = Array.Empty<IndexingTask>();

            do
            {
                // Create a scope for the content manager
                var shellScope = await _shellHost.GetScopeAsync(_shellSettings);

                await shellScope.UsingAsync(async scope =>
                {
                    // Load the next batch of tasks
                    batch = (await _indexingTaskManager.GetIndexingTasksAsync(lastTaskId, BatchSize)).ToArray();

                    if (!batch.Any())
                    {
                        return;
                    }

                    var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
                    var indexHandlers = scope.ServiceProvider.GetServices<IContentItemIndexHandler>();

                    // Pre-load all content items to prevent SELECT N+1
                    var updatedContentItemIds = batch
                        .Where(x => x.Type == IndexingTaskTypes.Update)
                        .Select(x => x.ContentItemId)
                        .ToArray();

                    var allPublished = await contentManager.GetAsync(updatedContentItemIds);
                    var allLatest = await contentManager.GetAsync(updatedContentItemIds, latest: true);

                    // Group all DocumentIndex by index to batch update them
                    var updatedDocumentsByIndex = new Dictionary<string, List<DocumentIndex>>();

                    foreach (var index in allIndices)
                    {
                        updatedDocumentsByIndex[index.Key] = new List<DocumentIndex>();
                    }

                    if (indexName != null)
                    {
                        indexSettingsList = indexSettingsList.Where(x => x.IndexName == indexName);
                    }

                    var needLatest = indexSettingsList.FirstOrDefault(x => x.IndexLatest) != null;
                    var needPublished = indexSettingsList.FirstOrDefault(x => !x.IndexLatest) != null;

                    var settingsByIndex = indexSettingsList.ToDictionary(x => x.IndexName, x => x);

                    foreach (var task in batch)
                    {
                        if (task.Type == IndexingTaskTypes.Update)
                        {
                            BuildIndexContext publishedIndexContext = null, latestIndexContext = null;

                            if (needPublished)
                            {
                                var contentItem = await contentManager.GetAsync(task.ContentItemId);
                                if (contentItem != null)
                                {
                                    publishedIndexContext = new BuildIndexContext(new DocumentIndex(task.ContentItemId), contentItem, new string[] { contentItem.ContentType });
                                    await indexHandlers.InvokeAsync(x => x.BuildIndexAsync(publishedIndexContext), _logger);
                                }
                            }

                            if (needLatest)
                            {
                                var contentItem = await contentManager.GetAsync(task.ContentItemId, VersionOptions.Latest);
                                if (contentItem != null)
                                {
                                    latestIndexContext = new BuildIndexContext(new DocumentIndex(task.ContentItemId), contentItem, new string[] { contentItem.ContentType });
                                    await indexHandlers.InvokeAsync(x => x.BuildIndexAsync(latestIndexContext), _logger);
                                }
                            }

                            // Update the document from the index if its lastIndexId is smaller than the current task id.
                            foreach (var index in allIndices)
                            {
                                if (index.Value >= task.Id || !settingsByIndex.TryGetValue(index.Key, out var settings))
                                {
                                    continue;
                                }

                                var context = !settings.IndexLatest ? publishedIndexContext : latestIndexContext;

                                //We index only if we actually found a content item in the database
                                if (context == null)
                                {
                                    //TODO purge these content items from IndexingTask table
                                    continue;
                                }

                                var cultureAspect = await contentManager.PopulateAspectAsync<CultureAspect>(context.ContentItem);
                                var culture = cultureAspect.HasCulture ? cultureAspect.Culture.Name : null;
                                var ignoreIndexedCulture = settings.Culture == "any" ? false : culture != settings.Culture;

                                // Ignore if the content item content type or culture is not indexed in this index
                                if (!settings.IndexedContentTypes.Contains(context.ContentItem.ContentType) || ignoreIndexedCulture)
                                {
                                    continue;
                                }

                                updatedDocumentsByIndex[index.Key].Add(context.DocumentIndex);
                            }
                        }
                    }

                    // Delete all the existing documents
                    foreach (var index in updatedDocumentsByIndex)
                    {
                        var deletedDocuments = updatedDocumentsByIndex[index.Key].Select(x => x.ContentItemId);

                        await _indexManager.DeleteDocumentsAsync(index.Key, deletedDocuments);
                    }

                    // Submits all the new documents to the index
                    foreach (var index in updatedDocumentsByIndex)
                    {
                        await _indexManager.StoreDocumentsAsync(index.Key, updatedDocumentsByIndex[index.Key]);
                    }

                    // Update task ids
                    lastTaskId = batch.Last().Id;

                    foreach (var indexStatus in allIndices)
                    {
                        if (indexStatus.Value < lastTaskId)
                        {
                            _indexingState.SetLastTaskId(indexStatus.Key, lastTaskId);
                        }
                    }

                    _indexingState.Update();
                });
            } while (batch.Length == BatchSize);
        }

        /// <summary>
        /// Creates a new index
        /// </summary>
        /// <returns></returns>
        public async Task CreateIndexAsync(ElasticIndexSettings indexSettings)
        {
            await _elasticIndexSettingsService.UpdateIndexAsync(indexSettings);
            await RebuildIndexAsync(indexSettings.IndexName);
        }

        /// <summary>
        /// Update an existing index
        /// </summary>
        /// <returns></returns>
        public Task UpdateIndexAsync(ElasticIndexSettings indexSettings)
        {
            return _elasticIndexSettingsService.UpdateIndexAsync(indexSettings);
        }

        /// <summary>
        /// Deletes permanently an index
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteIndexAsync(string indexName)
        {
            //Delete the Elastic Index first
            bool result = await _indexManager.DeleteIndex(indexName);

            if (result)
            {
                //Now delete it's setting
                await _elasticIndexSettingsService.DeleteIndexAsync(indexName);
            }
            return result;
        }

        /// <summary>
        /// Restarts the indexing process from the beginning in order to update
        /// current content items. It doesn't delete existing entries from the index.
        /// </summary>
        public void ResetIndex(string indexName)
        {
            _indexingState.SetLastTaskId(indexName, 0);
            _indexingState.Update();
        }

        /// <summary>
        /// Deletes and recreates the full index content.
        /// </summary>
        public async Task RebuildIndexAsync(string indexName)
        {
            await _indexManager.DeleteIndex(indexName);
            await _indexManager.CreateIndexAsync(indexName);
            ResetIndex(indexName);
        }

        public async Task<ElasticSettings> GetElasticSettingsAsync()
        {
            var siteSettings = await _siteService.GetSiteSettingsAsync();

            if (siteSettings.Has<ElasticSettings>())
            {
                return siteSettings.As<ElasticSettings>();
            }
            else
            {
                return new ElasticSettings();
            }
        }
    }
}
