using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Users.Workflows.Activities;
using OrchardCore.Users.Workflows.ViewModels;
using OrchardCore.Workflows.Display;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Users.Workflows.Drivers
{
    public class RegisterUserTaskDisplayDriver : ActivityDisplayDriver<RegisterUserTask, RegisterUserTaskViewModel>
    {
        private readonly IStringLocalizer S;

        public RegisterUserTaskDisplayDriver(IStringLocalizer<RegisterUserTaskDisplayDriver> localizer)
        {
            S = localizer;
        }

        protected override void EditActivity(RegisterUserTask activity, RegisterUserTaskViewModel model)
        {
            model.SendConfirmationEmail = activity.SendConfirmationEmail;
            model.ConfirmationEmailSubject = activity.ConfirmationEmailSubject.Expression;
            model.ConfirmationEmailTemplate = activity.ConfirmationEmailTemplate.Expression;
            model.RequireModeration = activity.RequireModeration;
        }

        public async override Task<IDisplayResult> UpdateAsync(RegisterUserTask model, IUpdateModel updater)
        {
            var viewModel = new RegisterUserTaskViewModel();
            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
            {
                model.SendConfirmationEmail = viewModel.SendConfirmationEmail;
                model.RequireModeration = viewModel.RequireModeration;
                model.ConfirmationEmailSubject = new WorkflowExpression<string>(viewModel.ConfirmationEmailSubject);
                model.ConfirmationEmailTemplate = new WorkflowExpression<string>(viewModel.ConfirmationEmailTemplate);

                if (model.SendConfirmationEmail)
                {
                    updater.ModelState.AddModelError(Prefix, nameof(viewModel.ConfirmationEmailSubject), S["A value is required for {0}.", nameof(viewModel.ConfirmationEmailSubject)]);
                    updater.ModelState.AddModelError(Prefix, nameof(viewModel.ConfirmationEmailTemplate), S["A value is required for {0}.", nameof(viewModel.ConfirmationEmailTemplate)]);
                }
            }

            return Edit(model);
        }
    }
}
