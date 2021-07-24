using System.IO;
using OrchardCore.Email;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Email
{
    public class MailMessageTests
    {
        [Fact]
        public void MailMessageDispose_DisposesItsAttachments()
        {
            // Arrange
            MailMessage message = null;
            
            //Act
            using(message = new MailMessage())
            {
                message.Attachments.Add(new MailMessageAttachment { Stream = new MemoryStream() });
                message.Attachments.Add(new MailMessageAttachment { Stream = new MemoryStream() });
            }

            // Assert
            Assert.All(message.Attachments, a => Assert.False(a.Stream.CanRead));
        }
    }
}
