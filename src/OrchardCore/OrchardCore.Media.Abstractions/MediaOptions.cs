using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OrchardCore.Media
{
    public class MediaOptions
    {
        /// <summary>
        /// The accepted sizes for custom width and height.
        /// When <see cref="UseTokenizedQueryString"/> is enabled all sizes are valid
        /// and this range acts as a helper for media profiles.
        /// </summary>
        // Setting a default value will make the IShellConfiguration add to the default values, rather than replace.
        public int[] SupportedSizes { get; set; }

        /// <summary>
        /// The list of allowed file extensions.
        /// </summary>
        // Setting a default value will make the IShellConfiguration add to the default values, rather than replace.
        public HashSet<string> AllowedFileExtensions { get; set; }

        /// <summary>
        /// The default number of days for the media cache control header.
        /// </summary>
        public int MaxBrowserCacheDays { get; set; }

        /// <summary>
        /// The maximum number of days a cached resized media item will be valid for, before being rebuilt on request.
        /// </summary>
        public int MaxCacheDays { get; set; }

        /// <summary>
        /// The maximum size of an uploaded file in bytes.
        /// NB: You might still need to configure the limit in IIS (https://docs.microsoft.com/en-us/iis/configuration/system.webserver/security/requestfiltering/requestlimits/)
        /// </summary>
        public int MaxFileSize { get; set; }

        /// <summary>
        /// A CDN base url that will be prefixed to the request path when serving images.
        /// </summary>
        public string CdnBaseUrl { get; set; }

        /// <summary>
        /// The request path used to route asset files.
        /// </summary>
        public PathString AssetsRequestPath { get; set; }

        /// <summary>
        /// The path used to store media assets. The path can be relative to the tenant's App_Data folder, or absolute.
        /// </summary>
        public string AssetsPath { get; set; }

        /// <summary>
        /// The folder under AssetsPath used to store users own media assets.
        /// </summary>
        public string AssetsUsersFolder { get; set; }        

        /// <summary>
        /// Encrypts the image processing query string to prevent disc filling.
        /// Defaults to <see langword="True"/>.
        /// </summary>
        public bool UseTokenizedQueryString { get; set; }

        /// <summary>
        /// The static file options used to serve non resized media.
        /// </summary>
        public StaticFileOptions StaticFileOptions { get; set; }

        public const string EncryptedCommandCacheKeyPrefix = "MediaCommands:";
    }
}
