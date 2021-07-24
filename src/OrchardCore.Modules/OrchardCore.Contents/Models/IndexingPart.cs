using OrchardCore.ContentManagement;

namespace OrchardCore.Contents.Models
{
    /// <summary>
    /// When attached to a content type, provides a way to edit the indexing
    /// properties of a content item.
    /// </summary>
    public class IndexingPart : ContentPart
    {
        public bool IsIndexed { get; set; }
    }
}
