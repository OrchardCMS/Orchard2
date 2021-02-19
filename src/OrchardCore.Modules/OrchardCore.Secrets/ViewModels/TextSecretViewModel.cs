
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.DisplayManagement.Handlers;

namespace OrchardCore.Secrets.ViewModels
{
    public class TextSecretViewModel
    {
        public string Text { get; set; }

        [BindNever]
        public BuildEditorContext Context { get; set; }
    }
}
