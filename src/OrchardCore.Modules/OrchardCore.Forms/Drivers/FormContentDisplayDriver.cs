using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Forms.Models;

namespace OrchardCore.Forms.Drivers
{
    public class FormContentDisplayDriver : ContentDisplayDriver
    {
        public override Task<IDisplayResult> DisplayAsync(ContentItem model, BuildDisplayContext context)
        {
            var formItemShape = context.Shape;
            // If the content item contains FormPart add Form Wrapper only in Display type Detail
            var formPart = model.As<FormPart>();
            if (formPart != null && context.DisplayType == "Detail")
            {
                // Add wrapper for content type
                formItemShape.Metadata.Wrappers.Add($"Form_Wrapper__{model.ContentType}");

                // Add wrapper for <form> tag
                formItemShape.Metadata.Wrappers.Add("Form_Wrapper");
            }

            // We don't need to return a shape result
            return Task.FromResult<IDisplayResult>(null);
        }
    }
}
