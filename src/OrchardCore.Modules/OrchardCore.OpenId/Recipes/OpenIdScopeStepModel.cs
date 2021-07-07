using System;
using System.Collections.Generic;
using System.Text;

namespace OrchardCore.OpenId.Recipes
{
    public class OpenIdScopeStepModel
    {
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public string ScopeName { get; set; }
        public string[] Resources { get; set; }
        public string[] TenantNames { get; set; }
    }
}
