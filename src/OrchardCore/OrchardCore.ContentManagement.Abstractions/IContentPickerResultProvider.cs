using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentManagement
{
    public interface IContentPickerResultProvider
    {
        string Name { get; }

        Task<string> GetContentPickerItemDescription(ContentItem contentItem, string pattern, string defaultValue);
        Task<IEnumerable<ContentPickerResult>> Search(ContentPickerSearchContext searchContext);
    }

    public class ContentPickerSearchContext
    {
        public string Query { get; set; }
        public bool DisplayAllContentTypes { get; set; }
        public IEnumerable<string> ContentTypes { get; set; }
        public string TitlePattern { get; set; }
        public string DescriptionPattern { get; set; }
        public ContentPartFieldDefinition PartFieldDefinition { get; set; }
    }

    public class ContentPickerResult
    {
        public string DisplayText { get; set; }
        public string Description { get; set; }
        public string ContentItemId { get; set; }
        public bool HasPublished { get; set; }
    }
}
