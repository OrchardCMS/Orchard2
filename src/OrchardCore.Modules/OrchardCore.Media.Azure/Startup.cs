using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.FileStorage.AzureBlob;
using OrchardCore.Media.Services;
using OrchardCore.Modules;

namespace OrchardCore.Media.Azure
{
    [Feature("OrchardCore.Media.Azure.Storage")]
    public class Startup : StartupBase
    {
        private IConfiguration _configuration;

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MediaBlobStorageOptions>(_configuration.GetSection("Modules:OrchardCore.Media.Azure"));

            // Only replace default implementation if options are valid.
            var connectionString = _configuration.GetValue<string>($"Modules:OrchardCore.Media.Azure:{nameof(MediaBlobStorageOptions.ConnectionString)}");
            var containerName = _configuration.GetValue<string>($"Modules:OrchardCore.Media.Azure:{nameof(MediaBlobStorageOptions.ContainerName)}");
            if (MediaBlobStorageOptionsCheckFilter.CheckOptions(connectionString, containerName))
            {
                services.Replace(ServiceDescriptor.Singleton<IMediaFileStore>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<MediaBlobStorageOptions>>().Value;
                    var clock = serviceProvider.GetRequiredService<IClock>();

                    var fileStore = new BlobFileStore(options, clock);

                    var mediaBaseUri = fileStore.BaseUri;
                    if (!String.IsNullOrEmpty(options.PublicHostName))
                        mediaBaseUri = new UriBuilder(mediaBaseUri) { Host = options.PublicHostName }.Uri;

                    return new MediaFileStore(fileStore, mediaBaseUri.ToString());
                }));
            }

            services.Configure<MvcOptions>((options) =>
            {
                options.Filters.Add(typeof(MediaBlobStorageOptionsCheckFilter));
            });
        }

        public override void Configure(IApplicationBuilder app, IRouteBuilder routes, IServiceProvider serviceProvider)
        {

        }
    }
}
