using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.FileStorage;
using OrchardCore.FileStorage.AzureBlob;
using OrchardCore.Media.Azure.Drivers;
using OrchardCore.Media.Azure.Services;
using OrchardCore.Modules;

namespace OrchardCore.Media.Azure
{
    [Feature("OrchardCore.Media.Azure.Storage")]
    public class Startup : StartupBase
    {
        private readonly ILogger<Startup> _logger;
        private readonly IShellConfiguration _configuration;

        public Startup(ILogger<Startup> logger, IShellConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override int Order => 10;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MediaBlobStorageOptions>(_configuration.GetSection("OrchardCore.Media.Azure"));

            // Only replace default implementation if options are valid.
            var connectionString = _configuration[$"OrchardCore.Media.Azure:{nameof(MediaBlobStorageOptions.ConnectionString)}"];
            var containerName = _configuration[$"OrchardCore.Media.Azure:{nameof(MediaBlobStorageOptions.ContainerName)}"];
            if (MediaBlobStorageOptionsCheckFilter.CheckOptions(connectionString, containerName, _logger))
            {
                // Register the media blob file cache provider.
                services.AddSingleton(serviceProvider =>
                {
                    var hostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();

                    if (String.IsNullOrWhiteSpace(hostingEnvironment.WebRootPath))
                    {
                        throw new Exception("The wwwroot folder for serving cache media files is missing.");
                    }

                    var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
                    var mediaBlobOptions = serviceProvider.GetRequiredService<IOptions<MediaBlobStorageOptions>>().Value;
                    var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                    var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                    var logger = serviceProvider.GetRequiredService<ILogger<MediaBlobFileCacheProvider>>();

                    var mediaCachePath = GetMediaCachePath(hostingEnvironment, shellSettings, mediaBlobOptions.AssetsCachePath);

                    if (!Directory.Exists(mediaCachePath))
                    {
                        Directory.CreateDirectory(mediaCachePath);
                    }

                    return new MediaBlobFileCacheProvider(logger, mediaOptions.AssetsRequestPath, mediaCachePath);
                });

                // Register a media cache file provider.
                services.AddSingleton<IMediaCacheFileProvider>(serviceProvider =>
                    serviceProvider.GetRequiredService<MediaBlobFileCacheProvider>());

                // Replace the default media file provider with the media cache file provider.
                services.Replace(ServiceDescriptor.Singleton<IMediaFileProvider>(serviceProvider =>
                    serviceProvider.GetRequiredService<MediaBlobFileCacheProvider>()));

                // Register the media cache file provider as a file store cache provider.
                services.AddSingleton<IMediaFileStoreCache>(serviceProvider =>
                    serviceProvider.GetRequiredService<MediaBlobFileCacheProvider>());

                // Replace the default media file store with a blob file store.
                services.Replace(ServiceDescriptor.Singleton<IMediaFileStore>(serviceProvider =>
                {
                    var blobStorageOptions = serviceProvider.GetRequiredService<IOptions<MediaBlobStorageOptions>>().Value;
                    var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                    var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                    var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
                    var clock = serviceProvider.GetRequiredService<IClock>();
                    var contentTypeProvider = serviceProvider.GetRequiredService<IContentTypeProvider>();
                    var fileStore = new BlobFileStore(blobStorageOptions, clock, contentTypeProvider);

                    var mediaPath = GetMediaPath(shellOptions.Value, shellSettings, mediaOptions.AssetsPath);

                    var mediaUrlBase = "/" + fileStore.Combine(shellSettings.RequestUrlPrefix, mediaOptions.AssetsRequestPath);

                    var originalPathBase = serviceProvider.GetRequiredService<IHttpContextAccessor>()
                        .HttpContext?.Features.Get<ShellContextFeature>()?.OriginalPathBase ?? null;

                    if (originalPathBase.HasValue)
                    {
                        mediaUrlBase = fileStore.Combine(originalPathBase, mediaUrlBase);
                    }

                    return new MediaFileStore(fileStore, mediaUrlBase, mediaOptions.CdnBaseUrl);
                }));
            }

            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(MediaBlobStorageOptionsCheckFilter));
            });
        }

        private string GetMediaPath(ShellOptions shellOptions, ShellSettings shellSettings, string assetsPath)
        {
            return PathExtensions.Combine(shellOptions.ShellsApplicationDataPath, shellOptions.ShellsContainerName, shellSettings.Name, assetsPath);
        }

        private string GetMediaCachePath(IHostingEnvironment hostingEnvironment, ShellSettings shellSettings, string assetsPath)
        {
            return PathExtensions.Combine(hostingEnvironment.WebRootPath, shellSettings.Name, assetsPath);
        }
    }


    [Feature("OrchardCore.Media.Azure.MediaCache")]
    public class MediaCacheStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDisplayDriver<MediaFileCache>, MediaBlobFileCacheDriver>();
            services.AddSingleton<IMediaCacheManagementProvider, MediaBlobCacheManagementProvider>();
        }
    }
}
