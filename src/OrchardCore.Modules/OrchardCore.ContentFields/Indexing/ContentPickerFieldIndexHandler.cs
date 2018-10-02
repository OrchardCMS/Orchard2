using System.Threading.Tasks;
using OrchardCore.ContentFields.Fields;
using OrchardCore.Indexing;

namespace OrchardCore.ContentFields.Indexing
{
    public class ContentPickerFieldIndexHandler : ContentFieldIndexHandler<ContentPickerField>
    {
        public override Task BuildIndexAsync(ContentPickerField field, BuildFieldIndexContext context)
        {
            var options = DocumentIndexOptions.Store;

            foreach (var contentItemId in field.ContentItemIds)
            {
                context.DocumentIndex.Set(context.Key, contentItemId, options);
            }

            return Task.CompletedTask;
        }
    }
}
