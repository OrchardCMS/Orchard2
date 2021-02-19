using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Secrets.ViewModels
{
    public class SecretBindingIndexViewModel
    {
        public List<SecretBindingEntry> SecretBindings { get; set; }
        public Dictionary<string, dynamic> Thumbnails { get; set; }
        public Dictionary<string, dynamic> Summaries { get; set; }
        public ContentOptions Options { get; set; } = new ContentOptions();
        public dynamic Pager { get; set; }
    }

    public class SecretBindingEntry
    {
        public string Name { get; set; }
        public SecretBinding SecretBinding { get; set; }
        public bool IsChecked { get; set; }
        public dynamic Summary { get; set; }
    }
    
    public class ContentOptions
    {
        public string Search { get; set; }
        public ContentsBulkAction BulkAction { get; set; }

        #region Lists to populate

        [BindNever]
        public List<SelectListItem> ContentsBulkAction { get; set; }

        #endregion Lists to populate
    }

    public enum ContentsBulkAction
    {
        None,
        Remove
    }    
}
