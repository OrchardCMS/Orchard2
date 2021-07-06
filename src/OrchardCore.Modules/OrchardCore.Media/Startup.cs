using System;
using System.IO;
using Fluid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Liquid.Tags;
using OrchardCore.Environment.Shell;
using OrchardCore.FileStorage;
using OrchardCore.FileStorage.FileSystem;
using OrchardCore.Indexing;
using OrchardCore.Liquid;
using OrchardCore.Media.Controllers;
using OrchardCore.Media.Core;
using OrchardCore.Media.Deployment;
using OrchardCore.Media.Drivers;
using OrchardCore.Media.Events;
using OrchardCore.Media.Fields;
using OrchardCore.Media.Filters;
using OrchardCore.Media.Handlers;
using OrchardCore.Media.Indexing;
using OrchardCore.Media.Liquid;
using OrchardCore.Media.Processing;
using OrchardCore.Media.Recipes;
using OrchardCore.Media.Services;
using OrchardCore.Media.Settings;
using OrchardCore.Media.Shortcodes;
using OrchardCore.Media.TagHelpers;
using OrchardCore.Media.ViewModels;
using OrchardCore.Modules;
using OrchardCore.Modules.FileProviders;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;
using OrchardCore.Shortcodes;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Providers;

namespace OrchardCore.Media
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;

        public Startup(IOptions<AdminOptions> adminOptions)
        {
            _adminOptions = adminOptions.Value;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAnchorTag, MediaAnchorTag>();

            services.Configure<TemplateOptions>(o =>
            {
                o.MemberAccessStrategy.Register<DisplayMediaFieldViewModel>();
                o.MemberAccessStrategy.Register<Anchor>();

                o.Filters.AddFilter("img_tag", MediaFilters.ImgTag);
            })
            .AddLiquidFilter<AssetUrlFilter>("asset_url")
            .AddLiquidFilter<ResizeUrlFilter>("resize_url");

            services.AddTransient<IConfigureOptions<MediaOptions>, MediaOptionsConfiguration>();

            services.AddSingleton<IMediaFileProvider>(serviceProvider =>
            {
                var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                var options = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;

                var mediaPath = GetMediaPath(shellOptions.Value, shellSettings, options.AssetsPath);

                if (!Directory.Exists(mediaPath))
                {
                    Directory.CreateDirectory(mediaPath);
                }
                return new MediaFileProvider(options.AssetsRequestPath, mediaPath);
            });

            services.AddSingleton<IStaticFileProvider, IMediaFileProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<IMediaFileProvider>()
            );

            services.AddSingleton<IMediaFileStore>(serviceProvider =>
            {
                var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
                var mediaEventHandlers = serviceProvider.GetServices<IMediaEventHandler>();
                var mediaCreatingEventHandlers = serviceProvider.GetServices<IMediaCreatingEventHandler>();
                var logger = serviceProvider.GetRequiredService<ILogger<DefaultMediaFileStore>>();

                var mediaPath = GetMediaPath(shellOptions.Value, shellSettings, mediaOptions.AssetsPath);
                var fileStore = new FileSystemStore(mediaPath);

                var mediaUrlBase = "/" + fileStore.Combine(shellSettings.RequestUrlPrefix, mediaOptions.AssetsRequestPath);

                var originalPathBase = serviceProvider.GetRequiredService<IHttpContextAccessor>()
                    .HttpContext?.Features.Get<ShellContextFeature>()?.OriginalPathBase ?? null;

                if (originalPathBase.HasValue)
                {
                    mediaUrlBase = fileStore.Combine(originalPathBase.Value, mediaUrlBase);
                }

                return new DefaultMediaFileStore(fileStore, mediaUrlBase, mediaOptions.CdnBaseUrl, mediaEventHandlers, mediaCreatingEventHandlers, logger);
            });

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IAuthorizationHandler, AttachedMediaFieldsFolderAuthorizationHandler>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            // ImageSharp

            // Add ImageSharp Configuration first, to override ImageSharp defaults.
            services.AddTransient<IConfigureOptions<ImageSharpMiddlewareOptions>, MediaImageSharpConfiguration>();

            services.AddImageSharp()
                .RemoveProvider<PhysicalFileSystemProvider>()
                .AddProvider<MediaResizingFileProvider>()
                .AddProcessor<ImageVersionProcessor>()
                .AddProcessor<TokenCommandProcessor>();

            services.AddScoped<MediaTokenSettingsUpdater>();
            services.AddScoped<IMediaTokenService, MediaTokenService>();
            services.AddTransient<IConfigureOptions<MediaTokenOptions>, MediaTokenOptionsConfiguration>();
            services.AddScoped<IFeatureEventHandler>(sp => sp.GetRequiredService<MediaTokenSettingsUpdater>());
            services.AddScoped<IModularTenantEvents>(sp => sp.GetRequiredService<MediaTokenSettingsUpdater>());

            // Media Field
            services.AddContentField<MediaField>()
                .UseDisplayDriver<MediaFieldDisplayDriver>();
            services.AddScoped<IContentPartFieldDefinitionDisplayDriver, MediaFieldSettingsDriver>();
            services.AddScoped<AttachedMediaFieldFileService, AttachedMediaFieldFileService>();
            services.AddScoped<IContentHandler, AttachedMediaFieldContentHandler>();
            services.AddScoped<IModularTenantEvents, TempDirCleanerService>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentFieldIndexHandler, MediaFieldIndexHandler>();
            services.AddScoped<IMediaFileTextProvider, PdfMediaFileTextProvider>();

            services.AddRecipeExecutionStep<MediaStep>();

            // MIME types
            services.TryAddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();

            services.AddTagHelpers<ImageTagHelper>();
            services.AddTagHelpers<ImageResizeTagHelper>();
            services.AddTagHelpers<AnchorTagHelper>();

            // Media Profiles
            services.AddScoped<MediaProfilesManager>();
            services.AddScoped<IMediaProfileService, MediaProfileService>();
            services.AddRecipeExecutionStep<MediaProfileStep>();

            // Media Name Normalizer
            services.AddScoped<IMediaNameNormalizerService, NullMediaNameNormalizerService>();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var mediaFileProvider = serviceProvider.GetRequiredService<IMediaFileProvider>();
            var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
            var mediaFileStoreCache = serviceProvider.GetService<IMediaFileStoreCache>();

            // FileStore middleware before ImageSharp, but only if a remote storage module has registered a cache provider.
            if (mediaFileStoreCache != null)
            {
                app.UseMiddleware<MediaFileStoreResolverMiddleware>();
            }

            // ImageSharp before the static file provider.
            app.UseImageSharp();

            // The file provider is a circular dependency and replaceable via di.
            mediaOptions.StaticFileOptions.FileProvider = mediaFileProvider;

            // Use services.PostConfigure<MediaOptions>() to alter the media static file options event handlers.
            app.UseStaticFiles(mediaOptions.StaticFileOptions);

            var adminControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Media.Index",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.MediaApplication",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/MediaApplication",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.MediaApplication) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.GetFolders",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/GetFolders",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.GetFolders) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.GetMediaItems",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/GetMediaItems",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.GetMediaItems) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.GetMediaItem",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/GetMediaItem",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.GetMediaItem) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.Upload",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/Upload",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Upload) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.DeleteFolder",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/DeleteFolder",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.DeleteFolder) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.DeleteMedia",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/DeleteMedia",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.DeleteMedia) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.MoveMedia",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/MoveMedia",
                defaults: new { controller = typeof(AdminController).ControllerName(), action = nameof(AdminController.MoveMedia) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.DeleteMediaList",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/DeleteMediaList",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.DeleteMediaList) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.MoveMediaList",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/MoveMediaList",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.MoveMediaList) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.CreateFolder",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/CreateFolder",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.CreateFolder) }
            );

            var mediaCacheControllerName = typeof(MediaCacheController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "MediaCache.Index",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaCache",
                defaults: new { controller = mediaCacheControllerName, action = nameof(MediaCacheController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "MediaCache.Purge",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaCache/Purge",
                defaults: new { controller = mediaCacheControllerName, action = nameof(MediaCacheController.Purge) }
            );

            routes.MapAreaControllerRoute(
                name: "Media.Options",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/Media/Options",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Options) }
            );

            var mediaProfilesControllerName = typeof(MediaProfilesController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "MediaProfiles.Index",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaProfiles",
                defaults: new { controller = mediaProfilesControllerName, action = nameof(MediaProfilesController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "MediaProfiles.Create",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaProfiles/Create",
                defaults: new { controller = mediaProfilesControllerName, action = nameof(MediaProfilesController.Create) }
            );

            routes.MapAreaControllerRoute(
                name: "MediaProfiles.Edit",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaProfiles/Edit",
                defaults: new { controller = mediaProfilesControllerName, action = nameof(MediaProfilesController.Edit) }
            );

            routes.MapAreaControllerRoute(
                name: "MediaProfiles.Delete",
                areaName: "OrchardCore.Media",
                pattern: _adminOptions.AdminUrlPrefix + "/MediaProfiles/Delete",
                defaults: new { controller = mediaProfilesControllerName, action = nameof(MediaProfilesController.Delete) }
            );
        }

        private string GetMediaPath(ShellOptions shellOptions, ShellSettings shellSettings, string assetsPath)
        {
            return PathExtensions.Combine(shellOptions.ShellsApplicationDataPath, shellOptions.ShellsContainerName, shellSettings.Name, assetsPath);
        }
    }

    [Feature("OrchardCore.Media.Cache")]
    public class MediaCacheStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, MediaCachePermissions>();
            services.AddScoped<INavigationProvider, MediaCacheAdminMenu>();
        }
    }

    [Feature("OrchardCore.Media.Slugify")]
    public class MediaSlugifyStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Media Name Normalizer
            services.AddScoped<IMediaNameNormalizerService, SlugifyMediaNameNormalizerService>();
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDeploymentSource, MediaDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<MediaDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, MediaDeploymentStepDriver>();

            services.AddTransient<IDeploymentSource, AllMediaProfilesDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<AllMediaProfilesDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, AllMediaProfilesDeploymentStepDriver>();
        }
    }

    [RequireFeatures("OrchardCore.Shortcodes")]
    public class ShortcodesStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Only add image as a descriptor as [media] is deprecated.
            services.AddShortcode<ImageShortcodeProvider>("image", d =>
            {
                d.DefaultValue = "[image] [/image]";
                d.Hint = "Add a image from the media library.";
                d.Usage =
@"[image]foo.jpg[/image]<br>
<table>
  <tr>
    <td>Args:</td>
    <td>width, height, mode</td>
  </tr>
  <tr>
    <td></td>
    <td>class, alt</td>
  </tr>
</table>";
                d.Categories = new string[] { "HTML Content", "Media" };
            });
        }
    }
}
