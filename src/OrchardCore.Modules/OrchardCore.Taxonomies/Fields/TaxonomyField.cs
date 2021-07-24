using System;
using System.Collections.Generic;
using OrchardCore.ContentManagement;

namespace OrchardCore.Taxonomies.Fields
{
    public class TaxonomyField : ContentField
    {
        public string TaxonomyContentItemId { get; set; }
        public string[] TermContentItemIds { get; set; } = Array.Empty<string>();
        // TermContentItemOrder stores the TermContentItemId (key) for every element in TermContentItemIds, and the item order on each of those terms (value).
        // TermContentItemIds was maintained as is, to prevent breaking changes.
        public Dictionary<string, int> TermContentItemOrder { get; set; } = new Dictionary<string, int>();
    }
}
