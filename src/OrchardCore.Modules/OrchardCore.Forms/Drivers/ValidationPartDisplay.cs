using System.Threading.Tasks;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Forms.Models;
using OrchardCore.Forms.ViewModels;

namespace OrchardCore.Forms.Drivers
{
    public class ValidationPartDisplay : ContentPartDisplayDriver<ValidationPart>
    {
        public override IDisplayResult Display(ValidationPart part)
        {
            // Empty Prefix on Display shapes
            Prefix = "";
            return View("ValidationPart", part).Location("Detail", "Content");
        }

        public override IDisplayResult Edit(ValidationPart part)
        {
            return Initialize<ValidationPartEditViewModel>("ValidationPart_Fields_Edit", m =>
            {
                m.For = part.For;
                m.ErrorMessage = part.ErrorMessage;
            });
        }

        public async override Task<IDisplayResult> UpdateAsync(ValidationPart part, IUpdateModel updater)
        {
            var viewModel = new ValidationPartEditViewModel();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
            {
                part.For = viewModel.For?.Trim();
                part.ErrorMessage = part.ErrorMessage?.Trim();
            }

            return Edit(part);
        }
    }
}
