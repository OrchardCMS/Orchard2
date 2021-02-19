
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Secrets.ViewModels
{
    public class SelectSecretViewModel
    {
        public string HtmlName { get; set; }

        [BindNever]
        public List<SelectListItem> Secrets { get; set; } = new List<SelectListItem>();
    }
}
