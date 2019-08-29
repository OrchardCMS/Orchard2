using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.Core
{
    public class MediaFileStoreCacheFileProvider : PhysicalFileProvider, IMediaFileProvider, IMediaFileStoreCacheFileProvider, IMediaFileStoreCache
    {
        /// <summary>
        /// The path in the wwwroot folder containing any asset cache.
        /// The tenants name will be appended to this folder path.
        /// </summary>
        public static readonly string AssetsCachePath = "ms-cache";

        // Use default stream copy buffer size to stay in gen0 garbage collection;
        private const int StreamCopyBufferSize = 81920;

        private readonly ILogger<MediaFileStoreCacheFileProvider> _logger;

        public MediaFileStoreCacheFileProvider(ILogger<MediaFileStoreCacheFileProvider> logger, PathString virtualPathBase, string root) : base(root)
        {
            _logger = logger;
            VirtualPathBase = virtualPathBase;
        }

        public MediaFileStoreCacheFileProvider(ILogger<MediaFileStoreCacheFileProvider> logger, PathString virtualPathBase, string root, ExclusionFilters filters) : base(root, filters)
        {
            _logger = logger;
            VirtualPathBase = virtualPathBase;
        }

        public PathString VirtualPathBase { get; }

        public Task<bool> IsCachedAsync(string path)
        {
            // Opportunity here to save metadata and/or provide cache validation / integrity checks.

            var fileInfo = GetFileInfo(path);
            return Task.FromResult(fileInfo.Exists);
        }

        public async Task SetCacheAsync(Stream stream, IFileStoreEntry fileStoreEntry, CancellationToken cancellationToken)
        {
            // File store semantics include a leading slash.
            var cachePath = Path.Combine(Root, fileStoreEntry.Path.Substring(1));
            var directory = Path.GetDirectoryName(cachePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = File.Create(cachePath))
            {
                await stream.CopyToAsync(fileStream, StreamCopyBufferSize, cancellationToken);
            }
        }

        public Task<bool> PurgeAsync()
        {
            var hasErrors = false;
            var folders = GetDirectoryContents(String.Empty);
            foreach (var fileInfo in folders)
            {
                if (fileInfo.IsDirectory)
                {
                    try
                    {
                        Directory.Delete(fileInfo.PhysicalPath, true);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting cache folder {Path}", fileInfo.PhysicalPath);
                        hasErrors = true;
                    }
                }
                else
                {
                    try
                    {
                        File.Delete(fileInfo.PhysicalPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting cache file {Path}", fileInfo.PhysicalPath);
                        hasErrors = true;
                    }
                }
            }

            return Task.FromResult(hasErrors);
        }
    }
}
