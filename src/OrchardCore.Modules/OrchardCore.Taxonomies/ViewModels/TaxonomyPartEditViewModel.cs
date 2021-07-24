using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.Taxonomies.Models;

namespace OrchardCore.Taxonomies.ViewModels
{
    public class TaxonomyPartEditViewModel
    {
        public string Hierarchy { get; set; }

        public string TermContentType { get; set; }

        public bool EnableOrdering { get; set; }

        [BindNever]
        public TaxonomyPart TaxonomyPart { get; set; }
    }
}
