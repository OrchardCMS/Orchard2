using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell.Descriptor.Models;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace OrchardCore.Google
{
    [Feature(GoogleConstants.Features.GoogleAuthentication)]
    public class AdminMenuGoogleAuthentication : INavigationProvider
    {
        private readonly ShellDescriptor _shellDescriptor;

        public AdminMenuGoogleAuthentication(
            IStringLocalizer<AdminMenuGoogleAuthentication> localizer,
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
                builder.Add(T["Google"], "15", settings => settings
                        .AddClass("google").Id("google")
                        .Add(T["Sign in with Google"], "10", client => client
                            .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = GoogleConstants.Features.GoogleAuthentication })
                            .Permission(Permissions.ManageGoogleAuthentication)
                            .LocalNav())
                    );
            }
            return Task.CompletedTask;
        }
    }
}
