using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell.Descriptor.Models;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace OrchardCore.Twitter
{
    [Feature(TwitterConstants.Features.TwitterLogin)]
    public class AdminMenuTwitterLogin : INavigationProvider
    {
        private readonly ShellDescriptor _shellDescriptor;

        public AdminMenuTwitterLogin(
            IStringLocalizer<AdminMenuTwitterLogin> localizer,
            ShellDescriptor shellDescriptor)
        {
            T = localizer;
            _shellDescriptor = shellDescriptor;
        }

        public IStringLocalizer T { get; set; }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(T["Twitter"], "15", settings => settings
                        .AddClass("twitter").Id("twitter")
                        .Add(T["Twitter Login"], "10", client => client
                            .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = TwitterConstants.Features.TwitterLogin })
                            .Permission(Permissions.ManageTwitterLogin)
                            .LocalNav())
                    );
            }
            return Task.CompletedTask;
        }
    }
}
