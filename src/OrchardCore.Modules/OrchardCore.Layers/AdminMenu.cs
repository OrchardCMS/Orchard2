using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.Layers.Drivers;

namespace OrchardCore.Layers
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
                .Add(S["Design"], design => design
                    .Add(S["Settings"], settings => settings
                        .Add(S["Zones"], S["Zones"], zones => zones
                            .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = LayerSiteSettingsDisplayDriver.GroupId })
                            .Permission(Permissions.ManageLayers)
                            .LocalNav()
                        ))
                    .Add(S["Widgets"], S["Widgets"], widgets => widgets
                        .Permission(Permissions.ManageLayers)
                        .Action("Index", "Admin", new { area = "OrchardCore.Layers" })
                        .LocalNav()
                    ));

            return Task.CompletedTask;
        }
    }
}
