using System;
using System.IO;

namespace OrchardCore.Email
{
    /// <summary>
    /// Represents a class that contains information for a mail message attachment.
    /// </summary>
    public class MailMessageAttachment : IDisposable
    {
        /// <summary>
        /// Gets or sets the attachment filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the attachment file stream.
        /// </summary>
        public Stream Stream { get; set; }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
