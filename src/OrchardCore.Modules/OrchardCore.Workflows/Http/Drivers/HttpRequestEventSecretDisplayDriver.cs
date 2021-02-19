using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Secrets;
using OrchardCore.Workflows.Http.ViewModels;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Services;
using OrchardCore.Workflows.Http.Activities;

namespace OrchardCore.Workflows.Http.Drivers
{
    public class HttpRequestEventSecretDisplayDriver : DisplayDriver<Secret, HttpRequestEventSecret>
    {
        private readonly IWorkflowTypeStore _workflowTypeStore;
        private readonly IStringLocalizer S;

        public HttpRequestEventSecretDisplayDriver(
            IWorkflowTypeStore workflowTypeStore,
            IStringLocalizer<HttpRequestEventSecretDisplayDriver> stringLocalizer)
        {
            _workflowTypeStore = workflowTypeStore;
            S = stringLocalizer;
        }

        public override IDisplayResult Display(HttpRequestEventSecret secret)
        {
            return Combine(
                View("HttpRequestEventSecret_Fields_Summary", secret).Location("Summary", "Content"),
                View("HttpRequestEventSecret_Fields_Thumbnail", secret).Location("Thumbnail", "Content"));
        }

        public override Task<IDisplayResult> EditAsync(HttpRequestEventSecret secret, BuildEditorContext context)
        {
            return Task.FromResult<IDisplayResult>(Initialize<HttpRequestEventSecretViewModel>("HttpRequestEventSecret_Fields_Edit", async model =>
            {
                model.WorkflowTypeId = secret.WorkflowTypeId;
                model.TokenLifeSpan = secret.TokenLifeSpan;

                model.WorkflowTypes = (await _workflowTypeStore.GetByStartActivityAsync(nameof(HttpRequestEvent)))
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.WorkflowTypeId }).ToList();

                model.Context = context;
            }).Location("Content"));
        }

        public override async Task<IDisplayResult> UpdateAsync(HttpRequestEventSecret secret, UpdateEditorContext context)
        {
            var model = new HttpRequestEventSecretViewModel();

            if (await context.Updater.TryUpdateModelAsync(model, Prefix))
            {
                if (String.IsNullOrEmpty(model.WorkflowTypeId))
                {
                    context.Updater.ModelState.AddModelError(Prefix, nameof(model.WorkflowTypeId), S["The Workflow type id value is required"]);
                }
                else
                {
                    var workflowType = await _workflowTypeStore.GetAsync(model.WorkflowTypeId);
                    if (workflowType == null)
                    {
                        context.Updater.ModelState.AddModelError(Prefix, nameof(model.WorkflowTypeId), S["The Workflow type id value is required"]);
                    }
                    else
                    {
                        var activity = workflowType.Activities.FirstOrDefault(a => String.Equals(a.Name, nameof(HttpRequestEvent), StringComparison.OrdinalIgnoreCase));
                        if (activity == null)
                        {
                            context.Updater.ModelState.AddModelError(Prefix, nameof(model.WorkflowTypeId), S["The Workflow type id value is required"]);
                        }
                        else
                        {
                            secret.ActivityId = activity.ActivityId;
                        }
                    }

                }

                secret.WorkflowTypeId = model.WorkflowTypeId;
                secret.TokenLifeSpan = model.TokenLifeSpan;
            }

            return await EditAsync(secret, context);
        }
    }
}
