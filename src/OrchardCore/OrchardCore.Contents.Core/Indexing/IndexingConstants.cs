namespace OrchardCore.Contents.Indexing
{
    public static class IndexingConstants
    {
        public const string OwnerKey = "Content.ContentItem.Owner";
        public const string AuthorKey = "Content.ContentItem.Author";
        public const string ContentTypeKey = "Content.ContentItem.ContentType";
        public const string ContentItemIdKey = "Content.ContentItem.ContentItemId";
        public const string ContentItemVersionIdKey = "Content.ContentItem.ContentItemVersionId";
        public const string CreatedUtcKey = "Content.ContentItem.CreatedUtc";
        public const string LatestKey = "Content.ContentItem.Latest";
        public const string ModifiedUtcKey = "Content.ContentItem.ModifiedUtc";
        public const string PublishedKey = "Content.ContentItem.Published";
        public const string PublishedUtcKey = "Content.ContentItem.PublishedUtc";
        public const string BodyAspectBodyKey = "Content.BodyAspect.Body";
        public const string DisplayTextAnalyzedKey = "Content.ContentItem.DisplayText.Analyzed";
        public const string DisplayTextKey = "Content.ContentItem.DisplayText";
        public const string DisplayTextNormalizedKey = "Content.ContentItem.DisplayText.Normalized";
        public const string FullTextKey = "Content.ContentItem.FullText";

        public static readonly string[] IgnoredFields = {
            "Analyzed",
            "Sanitize",
            "Normalized"
        };
    }
}
