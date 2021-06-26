using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Search.Elastic.ViewModels
{
    public class AdminIndexViewModel
    {
        public IEnumerable<IndexViewModel> Indexes { get; set; }

        public ContentOptions Options { get; set; } = new ContentOptions();

        [BindNever]
        public dynamic Pager { get; set; }
    }

    public class ContentOptions
    {
        public ContentsBulkAction BulkAction { get; set; }

        public string Search { get; set; }

        #region Lists to populate

        [BindNever]
        public List<SelectListItem> ContentsBulkAction { get; set; }

        #endregion Lists to populate
    }

    public enum ContentsBulkAction
    {
        None,
        Reset,
        Rebuild,
        Remove
    }
}
