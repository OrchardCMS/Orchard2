using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.FileStorage;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Media.Services
{
    /// <summary>
    /// Checks if the user has related permission to manage the path resource which passed from AuthorizationHandler
    /// </summary>
    public class ManageMediaFolderAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AttachedMediaFieldFileService _attachedMediaFieldFileService;
        private readonly IMediaFileStore _fileStore;
        private char _pathSeparator;
        private string _mediaFieldsFolder;
        private string _usersFolder;
        private readonly MediaOptions _mediaOptions;
        private Dictionary<string, Permission> folderPermissios = new Dictionary<string, Permission>();
        private readonly IUserAssetFolderNameProvider _userAssetFolderNameProvider;

        public ManageMediaFolderAuthorizationHandler(IServiceProvider serviceProvider,
            AttachedMediaFieldFileService attachedMediaFieldFileService,
            IMediaFileStore fileStore,
            IOptions<MediaOptions> options,
            IUserAssetFolderNameProvider userAssetFolderNameProvider)
        {
            _serviceProvider = serviceProvider;
            _attachedMediaFieldFileService = attachedMediaFieldFileService;
            _fileStore = fileStore;
            _mediaOptions = options.Value;
            _userAssetFolderNameProvider = userAssetFolderNameProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.HasSucceeded)
            {
                // This handler is not revoking any pre-existing grants.
                return;
            }

            if (requirement.Permission.Name != Permissions.ManageMediaFolder.Name)
            {
                return;
            }

            if (context.Resource == null)
            {
                return;
            }

            _pathSeparator = _fileStore.Combine("a", "b").Contains('/') ? '/' : '\\';

            // ensure end trailing slash
            _mediaFieldsFolder = _fileStore.NormalizePath(_attachedMediaFieldFileService.MediaFieldsFolder)
                                .TrimEnd(_pathSeparator) + _pathSeparator;

            _usersFolder = _fileStore.NormalizePath(_mediaOptions.AssetsUsersFolder)
                                .TrimEnd(_pathSeparator) + _pathSeparator;

            var path = context.Resource as string;

            string userOwnFolder = _fileStore.NormalizePath(
                                    _fileStore.Combine(_usersFolder, _userAssetFolderNameProvider.GetUserAssetFolderName(context.User)))
                                    .TrimEnd(_pathSeparator) + _pathSeparator;

            Permission permission = Permissions.ManageMedia;

            // handle attached media field folder
            if (IsAuthorizedFolder(_mediaFieldsFolder, path) || IsDescendantOfauthorizedFolder(_mediaFieldsFolder, path))
            {
                permission = Permissions.ManageAttachedMediaFieldsFolder;
            }

            if (IsAuthorizedFolder(_usersFolder, path) || IsAuthorizedFolder(userOwnFolder, path) || IsDescendantOfauthorizedFolder(userOwnFolder, path))
            {
                permission = Permissions.ManageOwnMedia;
            }

            if (IsDescendantOfauthorizedFolder(_usersFolder, path) && !IsAuthorizedFolder(userOwnFolder, path) && !IsDescendantOfauthorizedFolder(userOwnFolder, path))
            {
                permission = Permissions.ManageOthersMedia;
            }

            // Lazy load to prevent circular dependencies
            var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

            if (await authorizationService.AuthorizeAsync(context.User, permission))
            {
                context.Succeed(requirement);
            }
        }

        private bool IsAuthorizedFolder(string authorizedFolder, string childPath)
        {
            // ensure end trailing slash
            childPath = _fileStore.NormalizePath(childPath)
                        .TrimEnd(_pathSeparator) + _pathSeparator;

            return childPath.Equals(authorizedFolder);
        }

        private bool IsDescendantOfauthorizedFolder(string authorizedFolder, string childPath)
        {
            childPath = _fileStore.NormalizePath(childPath);
            return childPath.StartsWith(authorizedFolder, StringComparison.Ordinal);
        }
    }
}
