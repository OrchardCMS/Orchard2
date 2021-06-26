using System.Collections.Generic;

namespace OrchardCore.Indexing.Settings
{
    /// <summary>
    /// Represents the indexing settings for a content type.
    /// </summary>
    public class TypeIndexSettings
    {
        /// <summary>
        /// The list of indexes that this type should be included into.
        /// </summary>
        public List<TypeIndexEntry> Indexes { get; set; }
    }

    public class TypeIndexEntry
    {
        public const string Published = "published";
        public const string Latest = "latest";

        public string Name { get; set; }
        public string Version { get; set; }
    }
}
