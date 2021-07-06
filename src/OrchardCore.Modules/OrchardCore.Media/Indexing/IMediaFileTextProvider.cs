using System.IO;

namespace OrchardCore.Media.Indexing
{
    public interface IMediaFileTextProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// A separate method is necessary since an empty or null return value from <see cref="GetText(string, Stream)"/>
        /// can simply mean the file's textual content is empty.
        /// </para>
        /// </remarks>
        bool CanHandle(string path);

        string GetText(string path, Stream fileStream);
    }
}
