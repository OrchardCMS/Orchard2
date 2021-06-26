using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OrchardCore.Search.Elastic.Settings
{
    public class ContentPickerFieldElasticEditorSettings
    {
        public string Index { get; set; }

        [BindNever]
        public string[] Indices { get; set; }
    }
}
