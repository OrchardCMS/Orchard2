namespace OrchardCore.Email.Models
{
    public class EmailMessage
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        /// <summary>
        /// Comma or semicolon separated values.
        /// </summary>
        public string Recipients { get; set; }
        public string ReplyTo { get; set; }
        public string From { get; set; }
        /// <summary>
        /// Comma or semicolon separated values.
        /// </summary>
        public string Bcc { get; set; }
        /// <summary>
        /// Comma or semicolon separated values.
        /// </summary>
        public string Cc { get; set; }
    }
}