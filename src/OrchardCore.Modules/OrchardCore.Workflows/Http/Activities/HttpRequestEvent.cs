using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Http.Activities
{
    public class HttpRequestEvent : EventActivity
    {
        public static string EventName => nameof(HttpRequestEvent);

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer S;

        public HttpRequestEvent(
            IStringLocalizer<HttpRequestEvent> localizer,
            IHttpContextAccessor httpContextAccessor
        )
        {
            S = localizer;
            _httpContextAccessor = httpContextAccessor;
        }

        public override string Name => EventName;
        public override LocalizedString DisplayText => S["Http Request Event"];
        public override LocalizedString Category => S["HTTP"];

        public string HttpMethod
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string Url
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public bool ValidateAntiforgeryToken
        {
            get => GetProperty(() => true);
            set => SetProperty(value);
        }

        public int TokenLifeSpan
        {
            get => GetProperty(() => 0);
            set => SetProperty(value);
        }

        public string HttpRequestEventSecret
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public override bool CanExecute(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var httpRequest = httpContext.Request;
            var isMatch = String.Equals(HttpMethod, httpRequest.Method, StringComparison.OrdinalIgnoreCase);

            return isMatch;
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"]);
        }

        public override ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes("Done");
        }
    }
}
