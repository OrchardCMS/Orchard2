using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Documents;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Search.Elastic.Model;

namespace OrchardCore.Search.Elastic
{
    public class ElasticIndexSettingsService
    {
        public ElasticIndexSettingsService()
        {
        }

        /// <summary>
        /// Loads the index settings document from the store for updating and that should not be cached.
        /// </summary>
        public Task<ElasticIndexSettingsDocument> LoadDocumentAsync() => DocumentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Gets the index settings document from the cache for sharing and that should not be updated.
        /// </summary>
        public async Task<ElasticIndexSettingsDocument> GetDocumentAsync()
        {
            var document = await DocumentManager.GetOrCreateImmutableAsync();

            foreach (var name in document.ElasticIndexSettings.Keys)
            {
                document.ElasticIndexSettings[name].IndexName = name;
            }

            return document;
        }

        public async Task<IEnumerable<ElasticIndexSettings>> GetSettingsAsync()
        {
            return (await GetDocumentAsync()).ElasticIndexSettings.Values;
        }

        public async Task<ElasticIndexSettings> GetSettingsAsync(string indexName)
        {
            var document = await GetDocumentAsync();

            if (document.ElasticIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings;
            }

            return null;
        }

        public async Task<string> GetIndexAnalyzerAsync(string indexName)
        {
            var document = await GetDocumentAsync();

            if (document.ElasticIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings.AnalyzerName;
            }

            return ElasticSettings.StandardAnalyzer;
        }

        public async Task<string> LoadIndexAnalyzerAsync(string indexName)
        {
            var document = await LoadDocumentAsync();

            if (document.ElasticIndexSettings.TryGetValue(indexName, out var settings))
            {
                return settings.AnalyzerName;
            }

            return ElasticSettings.StandardAnalyzer;
        }

        public async Task UpdateIndexAsync(ElasticIndexSettings settings)
        {
            var document = await LoadDocumentAsync();
            document.ElasticIndexSettings[settings.IndexName] = settings;
            await DocumentManager.UpdateAsync(document);
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            var document = await LoadDocumentAsync();
            document.ElasticIndexSettings.Remove(indexName);
            await DocumentManager.UpdateAsync(document);
        }

        private static IDocumentManager<ElasticIndexSettingsDocument> DocumentManager =>
            ShellScope.Services.GetRequiredService<IDocumentManager<ElasticIndexSettingsDocument>>();
    }
}
