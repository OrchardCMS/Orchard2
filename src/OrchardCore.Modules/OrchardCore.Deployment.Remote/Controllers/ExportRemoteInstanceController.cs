using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.Deployment.Core.Services;
using OrchardCore.Deployment.Remote.Services;
using OrchardCore.Deployment.Remote.ViewModels;
using OrchardCore.Deployment.Services;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using OrchardCore.Secrets;
using YesSql;

namespace OrchardCore.Deployment.Remote.Controllers
{
    [Admin]
    public class ExportRemoteInstanceController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly IDeploymentManager _deploymentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISession _session;
        private readonly RemoteInstanceService _service;
        private readonly ISecretService<TextSecret> _textSecretService;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer H;

        public ExportRemoteInstanceController(
            IAuthorizationService authorizationService,
            ISession session,
            RemoteInstanceService service,
            IDeploymentManager deploymentManager,
            ISecretService<TextSecret> textSecretService,
            INotifier notifier,
            IHtmlLocalizer<ExportRemoteInstanceController> localizer)
        {
            _authorizationService = authorizationService;
            _deploymentManager = deploymentManager;
            _session = session;
            _service = service;
            _textSecretService = textSecretService;
            _notifier = notifier;
            H = localizer;
        }

        [HttpPost]
        public async Task<IActionResult> Execute(int id, string remoteInstanceId, string returnUrl)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.Export))
            {
                return Forbid();
            }

            var deploymentPlan = await _session.GetAsync<DeploymentPlan>(id);

            if (deploymentPlan == null)
            {
                return NotFound();
            }

            var remoteInstance = await _service.GetRemoteInstanceAsync(remoteInstanceId);

            if (remoteInstance == null)
            {
                return NotFound();
            }

            string archiveFileName;
            var filename = deploymentPlan.Name.ToSafeName() + ".zip";

            using (var fileBuilder = new TemporaryFileBuilder())
            {
                archiveFileName = PathExtensions.Combine(Path.GetTempPath(), filename);

                var deploymentPlanResult = new DeploymentPlanResult(fileBuilder, new RecipeDescriptor(), remoteInstance.RsaEncryptionSecret, remoteInstance.RsaSigningSecret);
                await _deploymentManager.ExecuteDeploymentPlanAsync(deploymentPlan, deploymentPlanResult);

                if (System.IO.File.Exists(archiveFileName))
                {
                    System.IO.File.Delete(archiveFileName);
                }

                ZipFile.CreateFromDirectory(fileBuilder.Folder, archiveFileName);
            }

            HttpResponseMessage response;

            try
            {
                using (var requestContent = new MultipartFormDataContent())
                {
                    requestContent.Add(new StreamContent(
                        new FileStream(archiveFileName,
                        FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.Asynchronous | FileOptions.SequentialScan)
                    ),
                        nameof(ImportViewModel.Content), Path.GetFileName(archiveFileName));
                    requestContent.Add(new StringContent(remoteInstance.ClientName), nameof(ImportViewModel.ClientName));

                    if (!String.IsNullOrEmpty(remoteInstance.ApiKeySecret))
                    {
                        var secret = await _textSecretService.GetSecretAsync(remoteInstance.ApiKeySecret);
                        requestContent.Add(new StringContent(secret.Text), nameof(ImportViewModel.ApiKey));
                    }
                    else
                    {
                        requestContent.Add(new StringContent(remoteInstance.ApiKey), nameof(ImportViewModel.ApiKey));
                    }

                    response = await _httpClient.PostAsync(remoteInstance.Url, requestContent);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _notifier.Success(H["Deployment executed successfully."]);
                }
                else
                {
                    _notifier.Error(H["An error occurred while sending the deployment to the remote instance: \"{0} ({1})\"", response.ReasonPhrase, (int)response.StatusCode]);
                }
            }
            finally
            {
                System.IO.File.Delete(archiveFileName);
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Display", "DeploymentPlan", new { area = "OrchardCore.Deployment", id });
        }
    }
}
