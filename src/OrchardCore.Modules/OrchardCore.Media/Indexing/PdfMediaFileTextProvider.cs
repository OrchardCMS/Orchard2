using System.IO;
using Cysharp.Text;
using UglyToad.PdfPig;

namespace OrchardCore.Media.Indexing
{
    public class PdfMediaFileTextProvider : IMediaFileTextProvider
    {
        public bool CanHandle(string path)
        {
            return path.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase);
        }

        public string GetText(string path, Stream fileStream)
        {
            if (!CanHandle(path))
            {
                return null;
            }

            using var document = PdfDocument.Open(fileStream);
            using var stringBuilder = ZString.CreateStringBuilder();

            foreach (var page in document.GetPages())
            {
                stringBuilder.Append(page.Text);
            }

            return stringBuilder.ToString();
        }
    }
}
