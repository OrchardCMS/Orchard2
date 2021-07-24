using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Forms.Models;
using OrchardCore.Forms.ViewModels;

namespace OrchardCore.Forms.Drivers
{
    public class ValidationRulePartDisplayDriver : ContentPartDisplayDriver<ValidationRulePart>
    {
        private readonly ValidationRuleOptions _validationRuleOptions;

        public ValidationRulePartDisplayDriver(IOptions<ValidationRuleOptions> validationRuleOptions)
        {
            _validationRuleOptions = validationRuleOptions.Value;
        }

        public override IDisplayResult Display(ValidationRulePart part,BuildPartDisplayContext context)
        {
            return View("ValidationRulePart", part).Location("Detail", "Content");
        }


        public override IDisplayResult Edit(ValidationRulePart part)
        {
            return Initialize<ValidationRulePartEditViewModel>("ValidationRulePart_Fields_Edit", m =>
            {
                m.Type = part.Type;
                m.Option = part.Option;
                m.ErrorMessage = part.ErrorMessage;
                m.ValidationRuleProviders = _validationRuleOptions.ValidationRuleProviders;
            });
        }

        public async override Task<IDisplayResult> UpdateAsync(ValidationRulePart part, IUpdateModel updater)
        {
            var viewModel = new ValidationRulePartEditViewModel();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
            {
                part.Type = viewModel.Type?.Trim();
                part.Option = viewModel.Option?.Trim();
                part.ErrorMessage = viewModel.ErrorMessage?.Trim();
            }

            return Edit(part);
        }
    }
}
