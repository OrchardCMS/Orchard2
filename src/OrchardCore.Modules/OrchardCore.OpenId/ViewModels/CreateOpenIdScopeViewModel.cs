using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace OrchardCore.OpenId.ViewModels
{
    public class CreateOpenIdScopeViewModel : IValidatableObject
    {
        public string Description { get; set; }

        [Required]
        public string DisplayName { get; set; }

        [Required]
        public string Name { get; set; }

        public string Resources { get; set; }

        public List<TenantEntry> Tenants { get; } = new List<TenantEntry>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var S = validationContext.GetService<IStringLocalizer<EditOpenIdScopeViewModel>>();

            if (Name.Contains(' '))
            {
                yield return new ValidationResult(S["The scope name cannot contain spaces."], new[] { nameof(Name) });
            }
        }

        public class TenantEntry
        {
            public bool Current { get; set; }
            public string Name { get; set; }
            public bool Selected { get; set; }
        }
    }
}
