using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;

namespace OrchardCore.Media
{
    /// <summary>
    /// A cache for media assets.
    /// </summary>
    public interface IMediaImageCache : IImageCache
    {
        /// <summary>
        /// Clears the media cache folder of all contents.
        /// </summary>
        Task ClearMediaCacheAsync();

        /// <summary>
        /// Try to set a cache file.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="extension"></param>
        /// <param name="stream"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Task TrySetAsync(string key, string extension, Stream stream, ImageMetaData metadata);

        /// <summary>
        /// Get a media cache file from the cache.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="extension">Request file extension</param>
        /// <returns></returns>
        Task<IMediaCacheFileResolver> GetMediaCacheFileAsync(string key, string fileExtension);
    }
}
