using System;
using Microsoft.AspNetCore.Http;

namespace OrchardCore.Media.Shortcodes
{
    public class MediaResolver
    {
        private readonly PathString requestPath;
        private readonly string cdnBase;
        private readonly IMediaFileStore mediaFileStore;

        public MediaResolver(PathString requestPath, string cdnBase, IMediaFileStore mediaFileStore)
        {
            this.requestPath = requestPath;
            this.cdnBase = cdnBase;
            this.mediaFileStore = mediaFileStore;
        }

        public string Resolve(string partialMediaPath)
        {

            if (IsSchemeless(partialMediaPath) || IsHttpOrHttps(partialMediaPath))
            {
                return partialMediaPath;
            }

            if (IsVirtualPath(partialMediaPath))
            {
                // add tenant path part
                return cdnBase + requestPath.Add(partialMediaPath[1..]).Value;
 
            }
            else
            {
                // add media store part
                return mediaFileStore.MapPathToPublicUrl(partialMediaPath);
            }
        }

        // Serve static files from virtual path (multi-tenancy)
        public static bool IsVirtualPath(string url) => url.StartsWith("~/", StringComparison.Ordinal);
        public static bool IsHttpOrHttps(string url) => url.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        public static bool IsSchemeless(string url) => url.StartsWith("//", StringComparison.Ordinal);
    }
}
