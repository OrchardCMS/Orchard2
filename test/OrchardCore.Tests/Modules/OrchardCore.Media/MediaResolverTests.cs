using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.FileStorage;
using OrchardCore.Media.Core;
using OrchardCore.Media.Events;
using OrchardCore.Media.Shortcodes;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Media
{
    public class MediaResolverTests
    {
        [Theory]
        [InlineData("fizz", "/buzz", "bar", "fizz/buzz/bar")]
        [InlineData("example-cdn", "/media", "doc", "example-cdn/media/doc")]
        [InlineData("", "/buzz", "bar", "/buzz/bar")]
        [InlineData("example-cdn", "/media", "https://example.com/bobs-your-uncle", "https://example.com/bobs-your-uncle")]
        [InlineData("example-cdn", "/media", "~/cs/customer/refund policy.odt", "example-cdn/tenant/cs/customer/refund policy.odt")]
        public void ResolveGivenPartialMediaPath(string cdnUrlPrefix, string path, string url, string expected)
        {
            var fileStore = new DefaultMediaFileStore(
                Mock.Of<IFileStore>(),
                path,
                cdnUrlPrefix,
                Enumerable.Empty<IMediaEventHandler>(),
                Enumerable.Empty<IMediaCreatingEventHandler>(),
                Mock.Of<ILogger<DefaultMediaFileStore>>());

            var httpContextRequestPath = new PathString("/tenant");

            var resolver = new MediaResolver(httpContextRequestPath, cdnUrlPrefix, fileStore);

            var actual = resolver.Resolve(url);

            Assert.Equal(expected, actual);
        }
    }
}
