using System.IO;

namespace OrchardCore.Email
{
    /// <summary>
    /// Represents a class that contains information for a mail message attachment.
    /// </summary>
    public class MailMessageAttachment
    {
        /// <summary>
        /// Gets or sets the attachment filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the attachment file stream.
        /// </summary>
        /// <remarks>The stream will not be closed after sending the email.</remarks>
        public Stream OpenStream { get; set; }
    }
}
