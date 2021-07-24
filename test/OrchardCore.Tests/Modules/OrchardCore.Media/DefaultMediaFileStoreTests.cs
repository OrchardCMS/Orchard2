using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.FileStorage;
using OrchardCore.Media.Core;
using OrchardCore.Media.Events;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Media
{
    public class DefaultMediaFileStoreTests
    {
        [Theory]
        [InlineData("", "baz", "/media/baz")]
        [InlineData("", "/media/baz", "/media/baz")]
        [InlineData("bobs_your_uncle", "baz", "bobs_your_uncle/media/baz")]
        [InlineData("bobs_your_uncle", "bobs_your_uncle/media/baz", "bobs_your_uncle/media/baz")]
        public void MappingUrl(string cdnBaseUrl, string path, string expected)
        {
            var fileStore = new DefaultMediaFileStore(
                 Mock.Of<IFileStore>(),
                 "/media",
                 cdnBaseUrl,
                 Enumerable.Empty<IMediaEventHandler>(),
                 Enumerable.Empty<IMediaCreatingEventHandler>(),
                 Mock.Of<ILogger<DefaultMediaFileStore>>());

            Assert.Equal(fileStore.MapPathToPublicUrl(path), expected);
        }
    }
}
