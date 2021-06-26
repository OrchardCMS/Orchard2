using System.Collections.Generic;

namespace OrchardCore.Search.Elastic.Indexing
{
    /// <summary>
    /// Represents the Lucene index settings of a property.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Gets or sets the data type in the Lucene index.
        /// </summary>
        public Types Type { get; set; }

        /// <summary>
        /// Gets or sets whether the field should be searchable or not. Default is <code>true</code>.
        /// </summary>
        public bool? Index { get; set; }

        /// <summary>
        /// Gets or sets whether the field value should be stored and retrievable. Default is <code>false</code>.
        /// </summary>
        public bool? Store { get; set; }

        /// <summary>
        /// Gets or sets whether the field value should included in the <code>_all</code> field. Default is <code>false</code>.
        /// </summary>
        public bool? IncludeInAll { get; set; }

        /// <summary>
        /// Gets or sets the scaling factor of the field. Values will be multiplied by this factor at index time
        /// and rounded to the closest value.
        /// </summary>
        public int? ScalingFactor { get; set; }

        /// <summary>
        /// Gets or sets the analyzer to be used at both index-time and search-time unless overridden by the
        /// <see cref="SearchAnalyzer"/>.
        /// </summary>
        public string Analyzer { get; set; }

        /// <summary>
        /// Gets or sets the analyzer to be used at search-time.
        /// </summary>
        public string SearchAnalyzer { get; set; }

        /// <summary>
        /// Gets the list of supplemental values to be indexed.
        /// </summary>
        public Dictionary<string, Property> Fields { get; } = new Dictionary<string, Property>();

        /// <summary>
        /// Gets or sets the query time boosting.
        /// </summary>
        public float? Boost { get; set; }
    }
}
