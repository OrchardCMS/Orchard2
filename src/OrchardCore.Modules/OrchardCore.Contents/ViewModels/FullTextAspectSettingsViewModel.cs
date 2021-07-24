namespace OrchardCore.Contents.ViewModels
{
    public class FullTextAspectSettingsViewModel
    {
        public bool IncludeFullTextTemplate { get; set; }
        public string FullTextTemplate { get; set; }
        public bool IncludeBodyAspect { get; set; }
        public bool IncludeDisplayText { get; set; }
        public bool ExcludeIndexing { get; set; }
    }
}
