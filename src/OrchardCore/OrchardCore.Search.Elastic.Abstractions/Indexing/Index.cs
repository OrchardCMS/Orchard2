using System.Collections.Generic;

namespace OrchardCore.Search.Elastic.Indexing
{
    /// <summary>
    /// Represents the settings of a Lucene index.
    /// </summary>
    public class Index
    {
        /// <summary>
        /// Gets or sets the technical name of the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the default analyzer.
        /// </summary>
        public string DefaultAnalyzer { get; set; }

        /// <summary>
        /// Gets or sets the list of default search fields.
        /// </summary>
        public string[] DefaultSearchFields { get; set; }

        /// <summary>
        /// Gets the list of types to index indexed by name.
        /// </summary>
        public Dictionary<string, Mapping> Mappings { get; } = new Dictionary<string, Mapping>();
    }
}
