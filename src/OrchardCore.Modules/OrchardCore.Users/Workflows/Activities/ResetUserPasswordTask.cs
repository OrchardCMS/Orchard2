using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using OrchardCore.Entities;
using OrchardCore.Settings;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using Microsoft.AspNetCore.Routing;
using OrchardCore.Email;
using OrchardCore.DisplayManagement.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Encodings.Web;

namespace OrchardCore.Users.Workflows.Activities
{
    public class ResetUserPasswordTask : TaskActivity
    {
        private readonly UserManager<IUser> _userManager;
        private readonly IUserService _userService;
        private readonly ISiteService _siteService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
        private readonly LinkGenerator _linkGenerator;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IStringLocalizer S;
        private readonly HtmlEncoder _htmlEncoder;

        public ResetUserPasswordTask(
            UserManager<IUser> userManager,
            IUserService userService,
            ISiteService siteService,
            IHttpContextAccessor httpContextAccessor,
            IWorkflowExpressionEvaluator expressionvaluator,
            LinkGenerator linkGenerator,
            IUpdateModelAccessor updateModelAccessor,
            IStringLocalizer<AssignUserRoleTask> localizer,
            HtmlEncoder htmlEncoder)
        {
            _userManager = userManager;
            _userService = userService;
            _siteService = siteService;
            _httpContextAccessor = httpContextAccessor;
            _expressionEvaluator = expressionvaluator;
            _linkGenerator = linkGenerator;
            _updateModelAccessor = updateModelAccessor;
            S = localizer;
            _htmlEncoder = htmlEncoder;
        }

        // The technical name of the activity. Activities on a workflow definition reference this name.
        public override string Name => nameof(ResetUserPasswordTask);

        public override LocalizedString DisplayText => S["Reset User Password Task"];

        // The category to which this activity belongs. The activity picker groups activities by this category.
        public override LocalizedString Category => S["User"];

        public WorkflowExpression<string> UserName
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> ResetPasswordEmailSubject
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        // The message to display.
        public WorkflowExpression<string> ResetPasswordEmailTemplate
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        // Returns the possible outcomes of this activity.
        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"], S["Invalid"]);
        }

        // This is the heart of the activity and actually performs the work to be done.
        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var userName = await _expressionEvaluator.EvaluateAsync(UserName, workflowContext, null);

            var user = await _userService.GetForgotPasswordUserAsync(userName) as User;
            if (user == null || ((await _siteService.GetSiteSettingsAsync()).As<RegistrationSettings>().UsersMustValidateEmail && !await _userManager.IsEmailConfirmedAsync(user)))
            {
                var updater = _updateModelAccessor.ModelUpdater;
                if (updater != null)
                {
                    updater.ModelState.TryAddModelError("", S["No email service is available"]);
                }
                return Outcomes("Invalid");
            }
            else
            {
                user.ResetToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.ResetToken));
                var resetPasswordUrl = _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext,
                    "ResetPassword", "ResetPassword", new { area = "OrchardCore.Users", code = user.ResetToken });

                workflowContext.Properties["ResetPasswordUrl"] = resetPasswordUrl;

                var subject = await _expressionEvaluator.EvaluateAsync(ResetPasswordEmailSubject, workflowContext, null);
                var body = await _expressionEvaluator.EvaluateAsync(ResetPasswordEmailTemplate, workflowContext, _htmlEncoder);
                var message = new MailMessage()
                {
                    To = user.Email,
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                var smtpService = _httpContextAccessor.HttpContext.RequestServices.GetService<ISmtpService>();

                if (smtpService == null)
                {
                    var updater = _updateModelAccessor.ModelUpdater;
                    if (updater != null)
                    {
                        updater.ModelState.TryAddModelError("", S["No email service is available"]);
                    }
                    return Outcomes("Invalid");
                }
                else
                {
                    var result = await smtpService.SendAsync(message);
                    if (!result.Succeeded)
                    {
                        var updater = _updateModelAccessor.ModelUpdater;
                        if (updater != null)
                        {
                            foreach (var item in result.Errors)
                            {
                                updater.ModelState.TryAddModelError(item.Name, item.Value);
                            }
                        }
                        return Outcomes("Invalid");
                    }
                }

                return Outcomes("Done");
            }
        }
    }
}
