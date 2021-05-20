using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Lists.Models;

namespace OrchardCore.Lists.ViewModels
{
    public class ListPartViewModel
    {
        public ListPartFilterViewModel ListPartFilterViewModel { get; set; }
        public ListPart ListPart { get; set; }
        public IEnumerable<ContentItem> ContentItems { get; set; }
        public IEnumerable<ContentTypeDefinition> ContainedContentTypeDefinitions { get; set; }
        public BuildPartDisplayContext Context { get; set; }
        public dynamic Pager { get; set; }
        public bool EnableOrdering { get; set; }

        [BindNever]
        public List<SelectListItem> ContentsBulkAction { get; set; }
        public ContentOptionsListViewModel Options { get; set; }
    }

    public class ContentOptionsListViewModel
    {
        public ContentOptionsListViewModel()
        {
            BulkAction = ContentsListBulkAction.None;
        }

        public ContentsListBulkAction BulkAction { get; set; }
    }

    public enum ContentsListBulkAction
    {
        None,
        PublishNow,
        Unpublish,
        Remove
    }
}
