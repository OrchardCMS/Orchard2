using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using OrchardCore.Workflows.ViewModels;
using YesSql;

namespace OrchardCore.Workflows.Controllers
{
    [Admin]
    public class ActivityController : Controller
    {
        private readonly ISession _session;
        private readonly IActivityLibrary _activityLibrary;
        private readonly IWorkflowManager _workflowManager;
        private readonly IActivityIdGenerator _activityIdGenerator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IActivityDisplayManager _activityDisplayManager;
        private readonly INotifier _notifier;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IHtmlLocalizer H;

        public ActivityController
        (
            ISession session,
            IActivityLibrary activityLibrary,
            IWorkflowManager workflowManager,
            IActivityIdGenerator activityIdGenerator,
            IAuthorizationService authorizationService,
            IActivityDisplayManager activityDisplayManager,
            INotifier notifier,
            IHtmlLocalizer<ActivityController> h,
            IUpdateModelAccessor updateModelAccessor)
        {
            _session = session;
            _activityLibrary = activityLibrary;
            _workflowManager = workflowManager;
            _activityIdGenerator = activityIdGenerator;
            _authorizationService = authorizationService;
            _activityDisplayManager = activityDisplayManager;
            _notifier = notifier;
            _updateModelAccessor = updateModelAccessor;
            H = h;
        }

        public async Task<IActionResult> Create(string activityName, int workflowTypeId, string returnUrl)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageWorkflows))
            {
                return Forbid();
            }

            var workflowType = await _session.GetAsync<WorkflowType>(workflowTypeId);
            var activity = _activityLibrary.InstantiateActivity(activityName);
            var activityId = _activityIdGenerator.GenerateUniqueId(new ActivityRecord());
            var activityEditor = await _activityDisplayManager.BuildEditorAsync(activity, _updateModelAccessor.ModelUpdater, isNew: true);

            activityEditor.Metadata.Type = "Activity_Edit";

            var viewModel = new ActivityEditViewModel
            {
                Activity = activity,
                ActivityId = activityId,
                ActivityEditor = activityEditor,
                WorkflowTypeId = workflowTypeId,
                WorkflowTypeUniqueId = workflowType.WorkflowTypeId,
                ReturnUrl = returnUrl
            };

            if (!activity.HasEditor)
            {
                // No editor to show; short-circuit to the "POST" action.
                return await Create(activityName, viewModel);
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string activityName, ActivityEditViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageWorkflows))
            {
                return Forbid();
            }

            var workflowType = await _session.GetAsync<WorkflowType>(model.WorkflowTypeId);
            var activity = _activityLibrary.InstantiateActivity(activityName);
            var activityEditor = await _activityDisplayManager.UpdateEditorAsync(activity, _updateModelAccessor.ModelUpdater, isNew: true);

            if (!ModelState.IsValid)
            {
                activityEditor.Metadata.Type = "Activity_Edit";
                model.Activity = activity;
                model.ActivityEditor = activityEditor;
                return View(model);
            }

            var activityRecord = new ActivityRecord
            {
                ActivityId = model.ActivityId,
                Name = activity.Name,
                Properties = activity.Properties,
            };
            workflowType.Activities.Add(activityRecord);

            _session.Save(workflowType);
            _notifier.Success(H["Activity added successfully."]);

            return Url.IsLocalUrl(model.ReturnUrl) ? (IActionResult)Redirect(model.ReturnUrl) : RedirectToAction("Edit", "WorkflowType", new { id = model.WorkflowTypeId });
        }

        public async Task<IActionResult> Edit(int workflowTypeId, string activityId, string returnUrl)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageWorkflows))
            {
                return Forbid();
            }

            var workflowType = await _session.GetAsync<WorkflowType>(workflowTypeId);
            var activityRecord = workflowType.Activities.Single(x => x.ActivityId == activityId);
            var activityContext = await _workflowManager.CreateActivityExecutionContextAsync(activityRecord, activityRecord.Properties);
            var activityEditor = await _activityDisplayManager.BuildEditorAsync(activityContext.Activity, _updateModelAccessor.ModelUpdater, isNew: false);

            activityEditor.Metadata.Type = "Activity_Edit";

            var viewModel = new ActivityEditViewModel
            {
                Activity = activityContext.Activity,
                ActivityId = activityId,
                ActivityEditor = activityEditor,
                WorkflowTypeId = workflowTypeId,
                WorkflowTypeUniqueId = workflowType.WorkflowTypeId,
                ReturnUrl = returnUrl
            };

            return View("EditActivity", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ActivityEditViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageWorkflows))
            {
                return Forbid();
            }

            var workflowType = await _session.GetAsync<WorkflowType>(model.WorkflowTypeId);
            var activityRecord = workflowType.Activities.Single(x => x.ActivityId == model.ActivityId);
            var activityContext = await _workflowManager.CreateActivityExecutionContextAsync(activityRecord, activityRecord.Properties);
            var activityEditor = await _activityDisplayManager.UpdateEditorAsync(activityContext.Activity, _updateModelAccessor.ModelUpdater, isNew: false);

            if (!ModelState.IsValid)
            {
                activityEditor.Metadata.Type = "Activity_Edit";
                model.Activity = activityContext.Activity;
                model.ActivityEditor = activityEditor;

                return View("EditActivity", model);
            }

            activityRecord.Properties = activityContext.Activity.Properties;

            _session.Save(workflowType);
            _notifier.Success(H["Activity updated successfully."]);

            return Url.IsLocalUrl(model.ReturnUrl)
                ? (IActionResult)Redirect(model.ReturnUrl)
                : RedirectToAction("Edit", "WorkflowType", new { id = model.WorkflowTypeId });
        }
    }
}
