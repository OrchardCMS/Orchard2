using System.Collections.Generic;

namespace OrchardCore.Search.Elastic.Indexing
{
    /// <summary>
    /// Represents the information of a content type in a Lucene index.
    /// </summary>
    public class Mapping
    {
        /// <summary>
        /// Gets the list properties to index indexed by name.
        /// </summary>
        public Dictionary<string, Property> Properties { get; } = new Dictionary<string, Property>();
    }
}
