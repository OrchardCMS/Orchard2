using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OrchardCore.Secrets.ViewModels
{
    public class SecretBindingViewModel
    {
        public string SecretId { get; set; }
        public string Name { get; set; }
        public string SelectedStore { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public dynamic Editor { get; set; }


        [BindNever]
        public IEnumerable<SecretStoreDescriptor> StoreEntries { get; set; }

        [BindNever]
        public Secret Secret { get; set; }
    }
}
