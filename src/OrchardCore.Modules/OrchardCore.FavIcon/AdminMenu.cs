using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.FavIcon.Drivers;

namespace OrchardCore.FavIcon
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
                .Add(S["Design"], security => security
                    .Add(S["Settings"], settings => settings
                        .Add(S["FavIcon"], S["FavIcon"].PrefixPosition(), registration => registration
                            .Permission(Permissions.ManageFavIconSettings)
                            .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = FavIconSettingsDisplayDriver.GroupId })
                            .LocalNav()
                        )));

            return Task.CompletedTask;
        }
    }
}
