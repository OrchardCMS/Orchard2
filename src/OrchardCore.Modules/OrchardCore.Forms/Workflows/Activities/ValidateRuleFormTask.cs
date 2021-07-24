using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.Forms.Drivers;
using OrchardCore.Forms.Helpers;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Forms.Workflows.Activities
{
    public class ValidateRuleFormTask : TaskActivity
    {
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ValidationRuleHelpers _validationRuleHelpers;
        private readonly ValidationRuleOptions _validationRuleOptions;
        private readonly IStringLocalizer S;

        public ValidateRuleFormTask(
            IHttpContextAccessor httpContextAccessor,
            IUpdateModelAccessor updateModelAccessor,
            IOptions<ValidationRuleOptions> validationRuleOptions,
            ValidationRuleHelpers validationRuleHelpers,
            IStringLocalizer<ValidateRuleFormTask> localizer
        )
        {
            _updateModelAccessor = updateModelAccessor;
            _httpContextAccessor = httpContextAccessor;
            _validationRuleHelpers = validationRuleHelpers;
            _validationRuleOptions = validationRuleOptions.Value;
            S = localizer;
        }

        public override string Name => nameof(ValidateRuleFormTask);

        public override LocalizedString DisplayText => S["Validate Rule Form Task"];

        public override LocalizedString Category => S["Validation"];

        public override bool HasEditor => false;

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Valid"], S["Invalid"]);
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var form = _httpContextAccessor.HttpContext.Request.Form;

            var contentItemId = form["ContentItemId"];

            if (String.IsNullOrEmpty(contentItemId)) throw new InvalidOperationException("Cannot find contentItem");
            var isValid = true;
            var validationRuleAspects = await _validationRuleHelpers.GetValidationRuleAspects(contentItemId);

            foreach (var item in form)
            {
                var validationRuleAspect = validationRuleAspects.FirstOrDefault(a => a.FormInputName == item.Key);
                if (validationRuleAspect != null)
                {
                    var validationRuleProvider = _validationRuleOptions.ValidationRuleProviders.FirstOrDefault(a => a.Name.Equals(validationRuleAspect.Type, StringComparison.OrdinalIgnoreCase));
                    var validationResult = validationRuleProvider.ValidateInputByRuleAsync(validationRuleAspect.Option, item.Value);
                    if (!validationResult)
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            var outcome = isValid ? "Valid" : "Invalid";
            return Outcomes(outcome);
        }

    }
}
