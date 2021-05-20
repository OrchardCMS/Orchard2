using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentManagement.Handlers
{
    public class ActivatingContentContext
    {
        public string ContentType { get; set; }
        public ContentTypeDefinition Definition { get; set; }
        public ContentItem ContentItem { get; set; }
    }
}
