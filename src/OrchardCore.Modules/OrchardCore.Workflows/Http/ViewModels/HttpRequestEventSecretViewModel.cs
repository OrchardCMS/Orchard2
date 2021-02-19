
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.DisplayManagement.Handlers;

namespace OrchardCore.Workflows.Http.ViewModels
{
    public class HttpRequestEventSecretViewModel
    {
        public string WorkflowTypeId { get; set; }
        public int TokenLifeSpan { get; set; }

        [BindNever]
        public List<SelectListItem> WorkflowTypes { get; set; }

        [BindNever]
        public BuildEditorContext Context { get; set; }
    }
}
