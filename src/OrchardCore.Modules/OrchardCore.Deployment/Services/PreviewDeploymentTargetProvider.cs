using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Deployment.Services
{
    public class PreviewDeploymentTargetProvider : IDeploymentTargetProvider
    {
        private readonly IStringLocalizer S;

        public PreviewDeploymentTargetProvider(IStringLocalizer<PreviewDeploymentTargetProvider> stringLocalizer)
        {
            S = stringLocalizer;
        }

        public Task<IEnumerable<DeploymentTarget>> GetDeploymentTargetsAsync()
        {
            return Task.FromResult<IEnumerable<DeploymentTarget>>(
                new[] {
                    new DeploymentTarget(
                        name: S["Preview"],
                        description: S["Preview the deployment plan in browser."],
                        route: new RouteValueDictionary(new
                        {
                            area = "OrchardCore.Deployment",
                            controller = "ExportFile",
                            action = "Preview",
                        })
                    )
                }
            );
        }
    }
}
