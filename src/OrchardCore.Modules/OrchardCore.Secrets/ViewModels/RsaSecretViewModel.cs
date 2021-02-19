
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.DisplayManagement.Handlers;

namespace OrchardCore.Secrets.ViewModels
{
    public class RsaSecretViewModel
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public RsaSecretType KeyType { get; set; }
        public bool CycleKey { get; set; }
        public string NewPublicKey  { get; set; }
        public string NewPrivateKey { get; set; }

        [BindNever]
        public List<SelectListItem> KeyTypes { get; set; }
        
        [BindNever]
        public BuildEditorContext Context { get; set; }
    }
}
