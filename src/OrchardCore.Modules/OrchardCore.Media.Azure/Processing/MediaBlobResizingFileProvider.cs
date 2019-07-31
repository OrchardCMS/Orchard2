using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.FileStorage.AzureBlob;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.Helpers;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace OrchardCore.Media.Azure.Processing
{
    public class MediaBlobResizingFileProvider : IImageProvider
    {
        private readonly IMediaFileStore _mediaStore;
        private readonly FormatUtilities _formatUtilities;
        private readonly int[] _supportedSizes;
        private readonly PathString _assetsRequestPath;

        public MediaBlobResizingFileProvider(
            IMediaFileStore mediaStore,
            IOptions<ImageSharpMiddlewareOptions> imageSharpOptions,
            IOptions<MediaOptions> mediaOptions
            )
        {
            _mediaStore = mediaStore;
            _formatUtilities = new FormatUtilities(imageSharpOptions.Value.Configuration);
            _supportedSizes = mediaOptions.Value.SupportedSizes;
            _assetsRequestPath = mediaOptions.Value.AssetsRequestPath;
        }

        /// <inheritdoc/>
        public Func<HttpContext, bool> Match { get; set; } = _ => true;

        /// <inheritdoc/>
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc/>
        public bool IsValidRequest(HttpContext context)
        {
            //TODO This method will be identical to the normal, so move to provider
            if (!context.Request.Path.StartsWithSegments(_assetsRequestPath))
            {
                return false;
            }

            if (_formatUtilities.GetExtensionFromUri(context.Request.GetDisplayUrl()) == null)
            {
                return false;
            }

            // Always allow ImageSharp to process an Image file from Blob, until have have a middleware to serve images (and possibly a cache too)
            //if (!context.Request.Query.ContainsKey(ResizeWebProcessor.Width) &&
            //    !context.Request.Query.ContainsKey(ResizeWebProcessor.Height))
            //{
            //    return false;
            //}

            if (context.Request.Query.TryGetValue(ResizeWebProcessor.Width, out var widthString))
            {
                var width = CommandParser.Instance.ParseValue<int>(widthString);

                if (Array.BinarySearch<int>(_supportedSizes, width) < 0)
                {
                    return false;
                }
            }

            if (context.Request.Query.TryGetValue(ResizeWebProcessor.Height, out var heightString))
            {
                var height = CommandParser.Instance.ParseValue<int>(heightString);

                if (Array.BinarySearch<int>(_supportedSizes, height) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<IImageResolver> GetAsync(HttpContext context)
        {
            //TODO consider this. It causes a HEAD request every time, checking if the files exists. Cache? Hmm BlobFile is CQRS so possible.

            // Path has already been correctly parsed before here.
            var filePath = _mediaStore.MapRequestPathToFileStorePath(context.Request.PathBase + context.Request.Path.Value);

            // Check to see if the file exists.
            var file = await _mediaStore.GetFileInfoAsync(filePath);

            var blobFile = file as BlobFile;
            if (blobFile == null || blobFile.BlobReference == null)
            {
                return null;
            }

            var metadata = new ImageMetaData(file.LastModifiedUtc);
            return new MediaBlobFileResolver(_mediaStore, blobFile, metadata);
        }
    }
}
