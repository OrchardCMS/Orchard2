using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json.Linq;
using OrchardCore.Admin;
using OrchardCore.Deployment.Core.Mvc;
using OrchardCore.Deployment.Core.Services;
using OrchardCore.Deployment.Services;
using OrchardCore.Deployment.Steps;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using YesSql;

namespace OrchardCore.Deployment.Controllers
{
    [Admin]
    public class ExportFileController : Controller
    {
        private readonly IDeploymentManager _deploymentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISession _session;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer H;

        public ExportFileController(
            IAuthorizationService authorizationService,
            ISession session,
            IDeploymentManager deploymentManager,
            INotifier notifier,
            IHtmlLocalizer<ExportFileController> htmlLocalizer
            )
        {
            _authorizationService = authorizationService;
            _deploymentManager = deploymentManager;
            _session = session;
            _notifier = notifier;
            H = htmlLocalizer;
        }

        [HttpPost]
        [DeleteFileResultFilter]
        public async Task<IActionResult> Execute(int id, string returnUrl)
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

            string archiveFileName;
            var filename = deploymentPlan.Name.ToSafeName() + ".zip";

            using (var fileBuilder = new TemporaryFileBuilder())
            {
                archiveFileName = PathExtensions.Combine(Path.GetTempPath(), filename);

                var recipeDescriptor = new RecipeDescriptor();
                var recipeFileDeploymentStep = deploymentPlan.DeploymentSteps.FirstOrDefault(ds => ds.Name == nameof(RecipeFileDeploymentStep)) as RecipeFileDeploymentStep;

                if (recipeFileDeploymentStep != null)
                {
                    recipeDescriptor.Name = recipeFileDeploymentStep.RecipeName;
                    recipeDescriptor.DisplayName = recipeFileDeploymentStep.DisplayName;
                    recipeDescriptor.Description = recipeFileDeploymentStep.Description;
                    recipeDescriptor.Author = recipeFileDeploymentStep.Author;
                    recipeDescriptor.WebSite = recipeFileDeploymentStep.WebSite;
                    recipeDescriptor.Version = recipeFileDeploymentStep.Version;
                    recipeDescriptor.IsSetupRecipe = recipeFileDeploymentStep.IsSetupRecipe;
                    recipeDescriptor.Categories = (recipeFileDeploymentStep.Categories ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                    recipeDescriptor.Tags = (recipeFileDeploymentStep.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                }

                var deploymentPlanResult = new DeploymentPlanResult(fileBuilder, recipeDescriptor);
                await _deploymentManager.ExecuteDeploymentPlanAsync(deploymentPlan, deploymentPlanResult);
                bool hasPlainTextSecrets = false;
                if (deploymentPlanResult.Secrets != null && deploymentPlanResult.Secrets.HasValues)
                {
                    hasPlainTextSecrets = deploymentPlanResult.Secrets.SelectTokens($"$..{nameof(RecipeSecret.Handler)}")
                        .Select(t => t.Value<string>())
                        .Any(v => String.Equals(v, RecipeSecretHandler.PlainText.ToString(), StringComparison.OrdinalIgnoreCase));
                }
                if (hasPlainTextSecrets)
                {
                    _notifier.Error(H["You cannot export a deployment plan containing plain text secrets to a file"]);
                    return LocalRedirect(returnUrl);
                }

                ZipFile.CreateFromDirectory(fileBuilder.Folder, archiveFileName);
            }

            return new PhysicalFileResult(archiveFileName, "application/zip") { FileDownloadName = filename };
        }
    }
}
