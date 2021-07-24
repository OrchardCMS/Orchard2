using System.Collections.Generic;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Forms.Models;

namespace OrchardCore.Forms.ViewModels
{
    public class ValidationRulePartEditViewModel : ShapeViewModel
    {
        public string Type { get; set; }
        public string Option { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<ValidationRuleProvider> ValidationRuleProviders { get;set;  }
    }
}
