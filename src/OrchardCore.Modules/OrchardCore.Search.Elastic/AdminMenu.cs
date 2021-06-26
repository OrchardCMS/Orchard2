using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Search.Elastic.Drivers;
using OrchardCore.Navigation;
using OrchardCore.Search.Elastic;

namespace OrchardCore.Search.Elastic
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
                .Add(S["Search"], "7", search => search
                    .AddClass("elasticsearch").Id("elasticsearch")
                    .Add(S["Indexing"], S["Indexing"].PrefixPosition(), import => import
                        .Add(S["Elastic Indices"], S["Elastic Indices"].PrefixPosition(), indexes => indexes
                            .Action("Index", "Admin", new { area = "OrchardCore.Search.Elastic" })
                            .Permission(Permissions.ManageIndexes)
                            .LocalNav())
                        .Add(S["Run Elastic Query"], S["Run Elastic Query"].PrefixPosition(), queries => queries
                            .Action("Query", "Admin", new { area = "OrchardCore.Search.Elastic" })
                            .Permission(Permissions.ManageIndexes)
                            .LocalNav()))
                    .Add(S["Settings"], settings => settings
                        .Add(S["Elastic Search"], S["Elastic Search"].PrefixPosition(), entry => entry
                             .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = ElasticSettingsDisplayDriver.GroupId })
                             .Permission(Permissions.ManageIndexes)
                             .LocalNav()
                        )));

            return Task.CompletedTask;
        }
    }
}
