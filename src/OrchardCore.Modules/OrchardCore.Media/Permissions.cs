using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Media
{
    public class Permissions : IPermissionProvider
    {
        public static readonly Permission ManageMedia = new Permission("ManageMediaContent", "Manage Media");
        public static readonly Permission ManageRootFolderMedia = new Permission("ManageRootFolderMediaContent", "Manage Media For Root Folder");
        public static readonly Permission ManageOwnMedia = new Permission("ManageOwnMediaContent", "Manage Own Media");
        public static readonly Permission ManageOthersMedia = new Permission("ManageOthersMediaContent", "Manage Media For Others");
        public static readonly Permission ManageOwnRoleMedia = new Permission("ManageOwnRoleMediaContent", "Manage Media For User Own Roles");
        public static readonly Permission ManageOthersRoleMedia = new Permission("ManageOthersRoleMediaContent", "Manage Media For Others Roles");
        public static readonly Permission ManageAttachedMediaFieldsFolder = new Permission("ManageAttachedMediaFieldsFolder", "Manage Attached Media Fields Folder");
        public static readonly Permission ManageMediaProfiles = new Permission("ManageMediaProfiles", "Manage Media Profiles");
        public static readonly Permission ViewMediaOptions = new Permission("ViewMediaOptions", "View Media Options");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[]
            {
                ManageMedia,
                ManageRootFolderMedia,
                ManageOwnMedia,
                ManageOthersMedia,
                ManageOwnRoleMedia,
                ManageOthersRoleMedia,
                ManageAttachedMediaFieldsFolder,
                ManageMediaProfiles,
                ViewMediaOptions
            }
            .AsEnumerable());
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] {
                        ManageMedia,
                        ManageRootFolderMedia,
                        ManageOwnMedia,
                        ManageOthersMedia,
                        ManageOwnRoleMedia,
                        ManageOthersRoleMedia,
                        ManageAttachedMediaFieldsFolder,
                        ManageMediaProfiles,
                        ViewMediaOptions }
                },
                new PermissionStereotype
                {
                    Name = "Editor",
                    Permissions = new[] { ManageMedia, ManageOwnMedia }
                },
                new PermissionStereotype
                {
                    Name = "Moderator",
                },
                new PermissionStereotype
                {
                    Name = "Author",
                    Permissions = new[] { ManageMedia, ManageOwnMedia } // Replace this by ManageOwnMedia when it's implemented
                },
                new PermissionStereotype
                {
                    Name = "Contributor",
                    Permissions = new[] { ManageMedia, ManageOwnMedia } // Replace this by ManageOwnMedia when it's implemented
                },
            };
        }
    }

    public class MediaCachePermissions : IPermissionProvider
    {
        public static readonly Permission ManageAssetCache = new Permission("ManageAssetCache", "Manage Asset Cache Folder");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[] { ManageAssetCache }.AsEnumerable());
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageAssetCache }
                }
            };
        }
    }
}
