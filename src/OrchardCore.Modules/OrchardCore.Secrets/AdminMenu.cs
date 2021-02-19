using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Secrets
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
                .Add(S["Configuration"], design => design
                    .Add(S["Secrets"], "Secrets".PrefixPosition(), import => import
                        .Action("Index", "Admin", new { area = "OrchardCore.Secrets" })
                        .Permission(Permissions.ManageSecrets)
                        .LocalNav()
                    )
                );

            return Task.CompletedTask;
        }
    }
}
