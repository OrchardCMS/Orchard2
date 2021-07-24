using OrchardCore.Users.Workflows.Activities;
using OrchardCore.Users.Workflows.ViewModels;
using OrchardCore.Workflows.Display;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Users.Workflows.Drivers
{
    public class ResetUserPasswordTaskDisplay : ActivityDisplayDriver<ResetUserPasswordTask, ResetUserPasswordTaskViewModel>
    {
        protected override void EditActivity(ResetUserPasswordTask activity, ResetUserPasswordTaskViewModel model)
        {
            model.UserName = model.UserName = activity.UserName.Expression;
            model.ResetPasswordEmailSubject = activity.ResetPasswordEmailSubject.Expression;
            model.ResetPasswordEmailTemplate = activity.ResetPasswordEmailTemplate.Expression;
        }

        protected override void UpdateActivity(ResetUserPasswordTaskViewModel model, ResetUserPasswordTask activity)
        {
            activity.UserName = new WorkflowExpression<string>(model.UserName);
            activity.ResetPasswordEmailSubject = new WorkflowExpression<string>(model.ResetPasswordEmailSubject);
            activity.ResetPasswordEmailTemplate = new WorkflowExpression<string>(model.ResetPasswordEmailTemplate);
        }
    }
}
