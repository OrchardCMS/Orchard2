using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Search.Elastic.ViewModels
{
    public class ElasticIndexSettingsViewModel
    {
        public string IndexName { get; set; }

        public string AnalyzerName { get; set; }

        public bool IndexLatest { get; set; }

        public string Culture { get; set; }

        public string[] IndexedContentTypes { get; set; }

        public bool IsCreate { get; set; }

        #region List to populate

        [BindNever]
        public IEnumerable<SelectListItem> Analyzers { get; set; }

        [BindNever]
        public IEnumerable<SelectListItem> Cultures { get; set; }

        #endregion List to populate
    }
}
