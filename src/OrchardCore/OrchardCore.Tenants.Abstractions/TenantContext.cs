using OrchardCore.Environment.Shell;

namespace OrchardCore.Tenants.Services
{
    /// <summary>
    /// Represents a class that contains tenant information.
    /// </summary>
    public class TenantContext
    {
        /// <summary>
        /// Gets or sets the settings shell.
        /// </summary>
        public ShellSettings ShellSettings { get; set; }

        /// <summary>
        /// Gets or sets the encoded url.
        /// </summary>
        public string EncodedUrl { get; set; }

    }
}
