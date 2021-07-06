using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Indexing;
using OrchardCore.Media.Fields;
using OrchardCore.Media.Settings;
using UglyToad.PdfPig;

namespace OrchardCore.Media.Indexing
{
    public class MediaFieldIndexHandler : ContentFieldIndexHandler<MediaField>
    {
        private readonly IMediaFileStore _mediaFileStore;
        private readonly IEnumerable<IMediaFileTextProvider> _mediaFileTextProviders;

        public MediaFieldIndexHandler(
            IMediaFileStore mediaFileStore,
            IEnumerable<IMediaFileTextProvider> mediaFileTextProviders)
        {
            _mediaFileStore = mediaFileStore;
            _mediaFileTextProviders = mediaFileTextProviders;
        }

        public async override Task BuildIndexAsync(MediaField field, BuildFieldIndexContext context)
        {
            var options = context.Settings.ToOptions();
            var settings = context.ContentPartFieldDefinition.GetSettings<MediaFieldSettings>();

            if (field.Paths.Length > 0)
            {
                if (settings.AllowMediaText)
                {
                    foreach (var key in context.Keys)
                    {
                        foreach (var mediaText in field.MediaTexts)
                        {
                            context.DocumentIndex.Set(key + ".MediaText", mediaText, options);
                        }
                    }
                }

                if (_mediaFileTextProviders.Any())
                {
                    // It doesn't really makes sense to store file contents without analyzing them for search as well.
                    var fileIndexingOptions = options | DocumentIndexOptions.Analyze;

                    foreach (var path in field.Paths)
                    {
                        using var fileStream = await _mediaFileStore.GetFileStreamAsync(path);
                        if (fileStream != null)
                        {
                            var fileTexts = _mediaFileTextProviders
                                .Where(provider => provider.CanHandle(path))
                                .Select(provider => provider.GetText(path, fileStream));

                            foreach (var fileText in fileTexts)
                            {
                                foreach (var key in context.Keys)
                                {
                                    context.DocumentIndex.Set(key + ".FileText", fileText, fileIndexingOptions);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var key in context.Keys)
                {
                    context.DocumentIndex.Set(key + ".MediaText", "NULL", options);
                    context.DocumentIndex.Set(key + ".FileText", "NULL", options);
                }
            }
        }
    }
}
