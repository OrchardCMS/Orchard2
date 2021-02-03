using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.FavIcon
{
    public class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageFavIconSettings =
            new Permission
            (
                "ManageFavIconSettings",
                "Manage FavIcon Settings"
            );

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
         => Task.FromResult(new[]
            {
                ManageFavIconSettings
            }
            .AsEnumerable());

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
         => new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageFavIconSettings }
                }
            };
    }
}
