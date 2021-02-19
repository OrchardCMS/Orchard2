using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;
using OrchardCore.Deployment.Core.Mvc;
using OrchardCore.Deployment.Core.Services;
using OrchardCore.Deployment.Models;
using OrchardCore.Deployment.Services;
using OrchardCore.Deployment.Steps;
using OrchardCore.Entities;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using OrchardCore.Settings;
using YesSql;

namespace OrchardCore.Deployment.Controllers
{
    [Admin]
    public class ExportFileController : Controller
    {
        private readonly IDeploymentManager _deploymentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISession _session;
        private readonly ISiteService _siteService;

        public ExportFileController(
            IAuthorizationService authorizationService,
            ISession session,
            IDeploymentManager deploymentManager,
            ISiteService siteService)
        {
            _authorizationService = authorizationService;
            _deploymentManager = deploymentManager;
            _session = session;
            _siteService = siteService;
        }

        [HttpPost]
        [DeleteFileResultFilter]
        public async Task<IActionResult> Execute(int id)
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

                var siteSettings = await _siteService.GetSiteSettingsAsync();
                var fileDownloadDeploymentTargetSettings = siteSettings.As<FileDownloadDeploymentTargetSettings>();

                var deploymentPlanResult = new DeploymentPlanResult(fileBuilder, recipeDescriptor, fileDownloadDeploymentTargetSettings.RsaEncryptionSecret, fileDownloadDeploymentTargetSettings.RsaSigningSecret );
                await _deploymentManager.ExecuteDeploymentPlanAsync(deploymentPlan, deploymentPlanResult);
                ZipFile.CreateFromDirectory(fileBuilder.Folder, archiveFileName);
            }

            return new PhysicalFileResult(archiveFileName, "application/zip") { FileDownloadName = filename };
        }
    }
}
