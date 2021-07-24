using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrchardCore.FileStorage;
using OrchardCore.Infrastructure.Html;
using OrchardCore.Media.Core;
using OrchardCore.Media.Events;
using OrchardCore.Media.Shortcodes;
using OrchardCore.ResourceManagement;
using OrchardCore.Shortcodes.Services;
using Shortcodes;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Media
{
    public class LinkShortcodeTests
    {
        [Theory]
        [InlineData("", "foo bar baz", "foo bar baz")]
        [InlineData("", "[share]", "[share]")]
        [InlineData("", "[link][/link]", "[link]")]
        [InlineData("", "[link url=design.doc]Design[/link]", @"<a href=""/media/design.doc"" download=""design.doc"">Design</a>")]
        [InlineData("", "[goto url=design.doc]Design[/goto]", @"[goto url=design.doc]Design[/goto]")]
        [InlineData("", "[link]/docs/testing.xml[/link]", @"<a href=""/media/docs/testing.xml"" download=""testing.xml"">/media/docs/testing.xml</a>")]
        [InlineData("", "[link url=/customer/support.odt]", @"<a href=""/media/customer/support.odt"" download=""support.odt"">/media/customer/support.odt</a>")]
        [InlineData("", "[link url=/customer/support.odt tooltip=\"Customer Support\" class=\"big bold\"]", @"<a title=""Customer Support"" class=""big bold"" href=""/media/customer/support.odt"" download=""support.odt"">/media/customer/support.odt</a>")]
        [InlineData("", "[link url=/customer/support.odt save=\"cs.odt\"]", @"<a href=""/media/customer/support.odt"" download=""cs.odt"">/media/customer/support.odt</a>")]
        [InlineData("", "[link url=http://example.com/customer/support.odt save=\"cs.odt\"]", @"<a href=""http://example.com/customer/support.odt"" download=""cs.odt"">http://example.com/customer/support.odt</a>")]
        [InlineData("", "[link url=//example_domain_server/customer/support.odt save=\"cs.odt\"]", @"<a href=""//example_domain_server/customer/support.odt"" download=""cs.odt"">//example_domain_server/customer/support.odt</a>")]
        [InlineData("", "[link url=~/customer/support.odt save=\"cs.odt\"]", @"<a href=""/tenant/customer/support.odt"" download=""cs.odt"">/tenant/customer/support.odt</a>")]
        [InlineData("", "[link url=\"~/customer/refund policy.odt\"]", @"<a href=""/tenant/customer/refund%20policy.odt"" download=""refund policy.odt"">/tenant/customer/refund policy.odt</a>")]
        [InlineData("bobs_your_uncle", "[link url=\"~/brother_bill/customer/refund policy.odt\"]", @"<a href=""bobs_your_uncle/tenant/brother_bill/customer/refund%20policy.odt"" download=""refund policy.odt"">bobs_your_uncle/tenant/brother_bill/customer/refund policy.odt</a>")]
        public async Task ShouldProcess(string cdnBaseUrl, string content, string expected)
        {
            var sanitizerOptions = new HtmlSanitizerOptions();
            sanitizerOptions.Configure.Add(opt =>
            {
                _ = opt.AllowedAttributes.Add("class");
                _ = opt.AllowedAttributes.Add("download");
            });

            var fileStore = new DefaultMediaFileStore(
                Mock.Of<IFileStore>(),
                "/media",
                cdnBaseUrl,
                Enumerable.Empty<IMediaEventHandler>(),
                Enumerable.Empty<IMediaCreatingEventHandler>(),
                Mock.Of<ILogger<DefaultMediaFileStore>>());

            var sanitizer = new HtmlSanitizerService(Options.Create(sanitizerOptions));
            
            var defaultHttpContext = new DefaultHttpContext();
            defaultHttpContext.Request.PathBase = new PathString("/tenant");
            var httpContextAccessor = Mock.Of<IHttpContextAccessor>(hca => hca.HttpContext == defaultHttpContext);

            var options = Options.Create(new ResourceManagementOptions { CdnBaseUrl = cdnBaseUrl });


            var linkProvider = new LinkShortcodeProvider(fileStore, sanitizer, httpContextAccessor, options);
            var processor = new ShortcodeService(new IShortcodeProvider[] { linkProvider }, Enumerable.Empty<IShortcodeContextProvider>());
            var processed = await processor.ProcessAsync(content);

            Assert.Equal(expected, processed);
        }
    }
}
