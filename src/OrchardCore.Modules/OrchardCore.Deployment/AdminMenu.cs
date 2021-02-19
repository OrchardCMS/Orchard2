using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Deployment
{
    public class AdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            builder
                .Add(S["Configuration"], configuration => configuration
                    .Add(S["Import/Export"], S["Import/Export"].PrefixPosition(), import => import
                        .Add(S["Settings"], settings => settings
                            .Add(S["File Download Settings"], S["File Download Settings"].PrefixPosition(), deployment => deployment
                                .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = "FileDownloadDeploymentTarget" })
                                .Permission(OrchardCore.Deployment.CommonPermissions.ManageDeploymentPlan)
                                .LocalNav()
                            )
                        )                    
                        .Add(S["Deployment Plans"], S["Deployment Plans"].PrefixPosition(), deployment => deployment
                            .Action("Index", "DeploymentPlan", new { area = "OrchardCore.Deployment" })
                            .Permission(Permissions.Export)
                            .LocalNav()
                        )
                        .Add(S["Package Import"], S["Package Import"].PrefixPosition(), deployment => deployment
                            .Action("Index", "Import", new { area = "OrchardCore.Deployment" })
                            .Permission(Permissions.Import)
                            .LocalNav()
                        )
                        .Add(S["JSON Import"], S["JSON Import"].PrefixPosition(), deployment => deployment
                            .Action("Json", "Import", new { area = "OrchardCore.Deployment" })
                            .Permission(Permissions.Import)
                            .LocalNav()
                        )
                    )
                );

            return Task.CompletedTask;
        }
    }
}
