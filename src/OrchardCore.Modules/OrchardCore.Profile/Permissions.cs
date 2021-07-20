using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Profile
{
    public class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageProfile = new Permission("ManageProfile", "Manage settings");

        // This permission is not exposed, it's just used for the APIs to generate/check custom ones
        public static readonly Permission ManageGroupProfile = new Permission("ManageResourceSettings", "Manage settings", new[] { ManageProfile });

        public IEnumerable<Permission> GetPermissions()
        {
            return new[] { ManageProfile };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new[] { ManageProfile }
                },
                new PermissionStereotype {
                    Name = "Editor",
                    Permissions = new[] { ManageProfile }
                },
                new PermissionStereotype {
                    Name = "Moderator",
                    Permissions = new[] { ManageProfile }
                },
                new PermissionStereotype {
                    Name = "Author",
                    Permissions = new[] { ManageProfile }
                },
                new PermissionStereotype {
                    Name = "Contributor",
                    Permissions = new[] { ManageProfile }
                },
                 new PermissionStereotype {
                    Name = "Authenticated",
                    Permissions = new[] { ManageProfile }
                },
                new PermissionStereotype {
                    Name = "Anonymous",
                    Permissions = new[] { ManageProfile }
                }
            };
        }

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(GetPermissions());
        }
    }
}
