using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Email
{
    /// <summary>
    /// Represents a settings for SMTP.
    /// </summary>
    public class SmtpSettings : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the default sender mail.
        /// </summary>
        [Required(AllowEmptyStrings = false), EmailAddress]
        public string DefaultSender { get; set; }

        /// <summary>
        /// Gets or sets the mail delivery method.
        /// </summary>
        [Required]
        public SmtpDeliveryMethod DeliveryMethod { get; set; }

        /// <summary>
        /// Gets or sets the mailbox directory, this used for <see cref="SmtpDeliveryMethod.SpecifiedPickupDirectory"/> option.
        /// </summary>
        public string PickupDirectoryLocation { get; set; }

        /// <summary>
        /// Gets or sets the SMTP server/host.
        /// </summary>
        public string Host { get; set; }


        /// <summary>
        /// Gets or sets the SMTP port number. Defaults to <c>25</c>.
        /// </summary>
        [Range(0, 65535)]
        public int Port { get; set; } = 25;

        /// <summary>
        /// Gets or sets whether the encryption is automatically selected.
        /// </summary>
        public bool AutoSelectEncryption { get; set; }

        /// <summary>
        /// Gets or sets whether the user credentials is required.
        /// </summary>
        public bool RequireCredentials { get; set; }

        /// <summary>
        /// Gets or sets whether to use the default user credentials.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets or sets the mail encryption method.
        /// </summary>
        public SmtpEncryptionMethod EncryptionMethod { get; set; }

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the user password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password secret.
        /// When a secret key is provided, it overrides the <see cref="Password"/> value.
        /// </summary>
        public string PasswordSecret { get; set; }

        /// <inheritdocs />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var S = validationContext.GetService<IStringLocalizer<SmtpSettings>>();

            switch (DeliveryMethod)
            {
                case SmtpDeliveryMethod.Network:
                    if (String.IsNullOrEmpty(Host))
                    {
                        yield return new ValidationResult(S["The {0} field is required.", "Host name"], new[] { nameof(Host) });
                    }
                    break;
                case SmtpDeliveryMethod.SpecifiedPickupDirectory:
                    if (String.IsNullOrEmpty(PickupDirectoryLocation))
                    {
                        yield return new ValidationResult(S["The {0} field is required.", "Pickup directory location"], new[] { nameof(PickupDirectoryLocation) });
                    }
                    break;
                default:
                    throw new NotSupportedException(S["The '{0}' delivery method is not supported.", DeliveryMethod]);
            }
        }
    }
}
