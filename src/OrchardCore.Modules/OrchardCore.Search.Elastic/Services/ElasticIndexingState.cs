using System.IO;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Search.Elastic
{
    /// <summary>
    /// This class persists the indexing state, a cursor, on the filesystem alongside the index itself.
    /// This state has to be on the filesystem as each node has its own local storage for the index.
    /// We will not need this at all
    /// </summary>
    public class ElasticIndexingState
    {
        private readonly string _indexSettingsFilename;
        private readonly JObject _content;

        public ElasticIndexingState(
            IOptions<ShellOptions> shellOptions,
            ShellSettings shellSettings
            )
        {
            _indexSettingsFilename = PathExtensions.Combine(
                shellOptions.Value.ShellsApplicationDataPath,
                shellOptions.Value.ShellsContainerName,
                shellSettings.Name,
                "elastic.status.json");

            if (!File.Exists(_indexSettingsFilename))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_indexSettingsFilename));

                File.WriteAllText(_indexSettingsFilename, new JObject().ToString(Newtonsoft.Json.Formatting.Indented));
            }

            _content = JObject.Parse(File.ReadAllText(_indexSettingsFilename));
        }

        public int GetLastTaskId(string indexName)
        {
            JToken value;
            if (_content.TryGetValue(indexName, out value))
            {
                return value.Value<int>();
            }
            else
            {
                lock (this)
                {
                    _content.Add(new JProperty(indexName, 0));
                }

                return 0;
            }
        }

        public void SetLastTaskId(string indexName, int taskId)
        {
            lock (this)
            {
                _content[indexName] = taskId;
            }
        }

        public void Update()
        {
            lock (this)
            {
                File.WriteAllText(_indexSettingsFilename, _content.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
    }
}
