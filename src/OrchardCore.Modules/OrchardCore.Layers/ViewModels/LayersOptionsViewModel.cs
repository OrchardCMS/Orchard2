using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Layers.ViewModels
{
    public class LayersOptionsViewModel
    {
        [BindNever]
        public List<SelectListItem> Cultures { get; set; }

        public string Culture { get; set; }
    }
}