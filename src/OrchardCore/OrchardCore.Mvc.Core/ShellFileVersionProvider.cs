using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Modules.FileProviders;

namespace OrchardCore.Mvc
{
    /// <summary>
    /// Provides version hash for a specified file.
    /// </summary>
    public class ShellFileVersionProvider : IFileVersionProvider
    {
        private const string VersionKey = "v";
        private static readonly char[] QueryStringAndFragmentTokens = new[] { '?', '#' };

        private readonly IEnumerable<IFileProvider> _fileProviders;
        private readonly IMemoryCache _cache;

        public ShellFileVersionProvider(
            IEnumerable<IStaticFileProvider> staticFileProviders,
            IHostingEnvironment environment,
            IMemoryCache cache
            )
        {
            _fileProviders = staticFileProviders
                .Concat(new[] { environment.WebRootFileProvider });
            _cache = cache;
        }

        public string AddFileVersionToPath(PathString requestPathBase, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var resolvedPath = path;

            var queryStringOrFragmentStartIndex = path.IndexOfAny(QueryStringAndFragmentTokens);
            if (queryStringOrFragmentStartIndex != -1)
            {
                resolvedPath = path.Substring(0, queryStringOrFragmentStartIndex);
            }

            if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                // Don't append version if the path is absolute.
                return path;
            }

            if (_cache.TryGetValue(path, out string value))
            {
                return value;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            foreach (var fileProvider in _fileProviders)
            {
                cacheEntryOptions.AddExpirationToken(fileProvider.Watch(resolvedPath));
                var fileInfo = fileProvider.GetFileInfo(resolvedPath);

                // Perform check against requestPathBase.
                if (!fileInfo.Exists &&
                    requestPathBase.HasValue &&
                    resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
                {
                    resolvedPath = resolvedPath.Substring(requestPathBase.Value.Length);
                    cacheEntryOptions.AddExpirationToken(fileProvider.Watch(resolvedPath));
                    fileInfo = fileProvider.GetFileInfo(resolvedPath);
                }

                // Perform check against VirtualPathBase.
                if (!fileInfo.Exists &&
                    fileProvider is IVirtualPathBaseProvider virtualPathBaseProvider &&
                    virtualPathBaseProvider.VirtualPathBase.HasValue &&
                    resolvedPath.StartsWith(virtualPathBaseProvider.VirtualPathBase.Value, StringComparison.OrdinalIgnoreCase))
                {
                    resolvedPath = resolvedPath.Substring(virtualPathBaseProvider.VirtualPathBase.Value.Length);
                    cacheEntryOptions.AddExpirationToken(fileProvider.Watch(resolvedPath));
                    fileInfo = fileProvider.GetFileInfo(resolvedPath);
                }

                if (fileInfo.Exists)
                {
                    value = QueryHelpers.AddQueryString(path, VersionKey, GetHashForFile(fileInfo));
                    cacheEntryOptions.SetSize(value.Length * sizeof(char));
                    value = _cache.Set(path, value, cacheEntryOptions);
                    return value;
                }
            }

            // If the file is not in the current server, set cache so no further checks are done.
            cacheEntryOptions.SetSize(path.Length * sizeof(char));
            value = _cache.Set(path, path, cacheEntryOptions);
            return value;
        }

        private static string GetHashForFile(IFileInfo fileInfo)
        {
            using (var sha256 = CryptographyAlgorithms.CreateSHA256())
            {
                using (var readStream = fileInfo.CreateReadStream())
                {
                    var hash = sha256.ComputeHash(readStream);
                    return WebEncoders.Base64UrlEncode(hash);
                }
            }
        }
    }
}
