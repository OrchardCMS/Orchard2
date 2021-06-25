using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using OrchardCore.Email;
using OrchardCore.Email.Services;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Email
{
    public class EmailTests
    {
        [Fact]
        public async Task SendEmail_WithDisplayName()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message"
            };

            await SendEmailAsync("Your Name <youraddress@host.com>", message);
        }

        [Fact]
        public async Task SendEmail_UsesDefaultSender()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message"
            };
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            Assert.Contains("From: Your Name <youraddress@host.com>", content);
        }

        [Fact]
        public async Task SendEmail_UsesCustomSender()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message",
                From = "My Name <youraddress@host.com>",
            };
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            Assert.Contains("From: My Name <youraddress@host.com>", content);
            Assert.Contains("Sender: Your Name <youraddress@host.com>", content);
        }

        [Fact]
        public async Task SendEmail_UsesCustomAuthorAndSender()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message",
                Sender = "Hisham Bin Ateya <hishamco_2007@hotmail.com>",
            };
            var content = await SendEmailAsync("Sebastien Ros <sebastienros@gmail.com>", message);

            Assert.Contains("From: Sebastien Ros <sebastienros@gmail.com>", content);
            Assert.Contains("Sender: Hisham Bin Ateya <hishamco_2007@hotmail.com>", content);
        }

        [Fact]
        public async Task SendEmail_UsesMultipleAuthors()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message",
                From = "sebastienros@gmail.com,hishamco_2007@hotmail.com"
            };
            var content = await SendEmailAsync("Hisham Bin Ateya <hishamco_2007@hotmail.com>", message);

            Assert.Contains("From: sebastienros@gmail.com, hishamco_2007@hotmail.com", content);
            Assert.Contains("Sender: Hisham Bin Ateya <hishamco_2007@hotmail.com>", content);
        }

        [Fact]
        public async Task SendEmail_UsesReplyTo()
        {
            var message = new MailMessage
            {
                To = "Hisham Bin Ateya <hishamco_2007@hotmail.com>",
                Subject = "Test",
                Body = "Test Message",
                From = "Hisham Bin Ateya <hishamco_2007@hotmail.com>",
                ReplyTo = "Hisham Bin Ateya <hishamco_2007@yahoo.com>",
            };
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            Assert.Contains("From: Hisham Bin Ateya <hishamco_2007@hotmail.com>", content);
            Assert.Contains("Reply-To: Hisham Bin Ateya <hishamco_2007@yahoo.com>", content);
        }

        [Fact]
        public async Task ReplyTo_ShouldHaveAuthors_IfNotSet()
        {
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message",
                From = "Sebastien Ros <sebastienros@gmail.com>"
            };
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            Assert.Contains("From: Sebastien Ros <sebastienros@gmail.com>", content);
            Assert.Contains("Reply-To: Sebastien Ros <sebastienros@gmail.com>", content);
        }

        [Theory]
        [InlineData("me <mailbox@domain.com>", "me", "mailbox@domain.com")]
        [InlineData("me<mailbox@domain.com>", "me", "mailbox@domain.com")]
        [InlineData("me     <mailbox@domain.com>", "me", "mailbox@domain.com")]
        [InlineData("<mailbox@domain.com>", "", "mailbox@domain.com")]
        [InlineData("mailbox@domain.com", "", "mailbox@domain.com")]
        [InlineData("(comment)mailbox(comment)@(comment)domain.com(me) ", "me", "mailbox@domain.com")]
        [InlineData("Sébastien <sébastien@domain.com>", "Sébastien", "sébastien@domain.com")]
        public void MailBoxAddress_ShouldParseEmail(string text, string name, string address)
        {
            Assert.True(MailboxAddress.TryParse(text, out var mailboxAddress));
            Assert.Equal(name, mailboxAddress.Name);
            Assert.Equal(address, mailboxAddress.Address);
        }

        [Fact]
        public async Task SendEmail_WithAttachment_FromFile()
        {
            // Arrange
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message"
            };
            message.Attachments.Add(new MailMessageAttachment { Filename = "test.txt" });

            await File.WriteAllTextAsync("test.txt", "Simple text ...");

            // Arrange
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            // Assert
            Assert.Contains("Content-Disposition: attachment; filename=test.txt", content);

            File.Delete("test.txt");
        }

        [Fact]
        public async Task SendEmail_WithAttachment_FromStream()
        {
            // Arrange
            var message = new MailMessage
            {
                To = "info@oc.com",
                Subject = "Test",
                Body = "Test Message"
            };
            var data = Encoding.UTF8.GetBytes("Simple text ...");
            message.Attachments.Add(new MailMessageAttachment { Filename = "test.txt", Stream = new MemoryStream(data) });

            // Arrange
            var content = await SendEmailAsync("Your Name <youraddress@host.com>", message);

            // Assert
            Assert.Contains("Content-Disposition: attachment; filename=test.txt", content);

            File.Delete("test.txt");
        }

        private async Task<string> SendEmailAsync(string defaultSender, MailMessage message)
        {
            var pickupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Email");

            if (Directory.Exists(pickupDirectoryPath))
            {
                var directory = new DirectoryInfo(pickupDirectoryPath);
                directory.GetFiles().ToList().ForEach(f => f.Delete());
            }

            Directory.CreateDirectory(pickupDirectoryPath);

            var options = new Mock<IOptions<SmtpSettings>>();
            options.Setup(o => o.Value).Returns(new SmtpSettings
            {
                DefaultSender = defaultSender,
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = pickupDirectoryPath
            });
            var logger = new Mock<ILogger<SmtpService>>();
            var localizer = new Mock<IStringLocalizer<SmtpService>>();
            var smtp = new SmtpService(options.Object, logger.Object, localizer.Object);
            var result = await smtp.SendAsync(message);

            Assert.True(result.Succeeded);

            var file = new DirectoryInfo(pickupDirectoryPath).GetFiles().FirstOrDefault();

            Assert.NotNull(file);

            var content = File.ReadAllText(file.FullName);

            return content;
        }
    }
}
