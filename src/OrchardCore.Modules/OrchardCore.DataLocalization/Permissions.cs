using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.DataLocalization
{
    /// <summary>
    /// Represents the localization module permissions.
    /// </summary>
    public class Permissions : IPermissionProvider
    {
        /// <summary>
        /// Gets a permission for managing the cultures.
        /// </summary>
        public static readonly Permission ManageLocalization = new Permission("ManageLocalization", "Manage dynamic localizations");

        /// <inheritdocs />
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[] { ManageLocalization }.AsEnumerable());
        }

        /// <inheritdocs />
        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageLocalization }
                },
                new PermissionStereotype
                {
                    Name = "Editor",
                    Permissions = new[] { ManageLocalization }
                }
            };
        }
    }
}
