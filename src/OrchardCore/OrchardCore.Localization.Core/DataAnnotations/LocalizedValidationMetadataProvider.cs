using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Localization.DataAnnotations
{
    /// <summary>
    /// Provides a validation for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public class LocalizedValidationMetadataProvider : IValidationMetadataProvider
    {
        private readonly IStringLocalizer _stringLocalizer;

        /// <summary>
        /// Initializes a new instance of a <see cref="LocalizedValidationMetadataProvider"/> with string localizer.
        /// </summary>
        /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/>.</param>
        public LocalizedValidationMetadataProvider(IStringLocalizer stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        /// <remarks>This will localize the default data annotations error message if it is exist, otherwise will try to look for a parameterized version.</remarks>
        /// <example>
        /// A property named 'UserName' that decorated with <see cref="RequiredAttribute"/> will be localized using
        /// "The {0} field is required." and "The UserName field is required." error messages.
        /// </example>
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var metadata in context.ValidationMetadata.ValidatorMetadata)
            {
                if (metadata is ValidationAttribute attribute)
                {
                    var displayName = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault()?.Name;
                    // Use DisplayName if present
                    var argument = displayName ?? context.Key.Name;
                    var errorMessageString = attribute.ErrorMessage == null && attribute.ErrorMessageResourceName == null
                        ? attribute.FormatErrorMessage(argument)
                        : attribute.ErrorMessage;

                    // Localize the parameterized error message
                    var localizedString = _stringLocalizer[errorMessageString];

                    if (localizedString == errorMessageString)
                    {
                        // Localize the unparameterized error message
                        var unparameterizedErrorMessage = errorMessageString.Replace(argument, "{0}");
                        localizedString = _stringLocalizer[unparameterizedErrorMessage];
                    }

                    attribute.ErrorMessage = localizedString;
                }
            }
        }
    }
}
