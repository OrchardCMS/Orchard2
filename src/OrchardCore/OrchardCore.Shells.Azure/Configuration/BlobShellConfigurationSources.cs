using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.FileStorage;
using OrchardCore.Shells.Azure.Services;

namespace OrchardCore.Shells.Azure.Configuration
{
    public class BlobShellConfigurationSources : IShellConfigurationSources
    {
        private readonly IShellsFileStore _shellsFileStore;
        private readonly BlobShellStorageOptions _blobOptions;
        private readonly string _container;
        private readonly string _fileStoreContainer;

        public BlobShellConfigurationSources(
            IShellsFileStore shellsFileStore,
            BlobShellStorageOptions blobOptions,
            IOptions<ShellOptions> shellOptions)
        {
            _shellsFileStore = shellsFileStore;
            _blobOptions = blobOptions;

            // e.g., Sites.
            _container = shellOptions.Value.ShellsContainerName;

            // e.g., App_Data/Sites
            _fileStoreContainer = Path.Combine(shellOptions.Value.ShellsApplicationDataPath, shellOptions.Value.ShellsContainerName);
        }

        public async Task AddSourcesAsync(string tenant, IConfigurationBuilder builder)
        {
            var appSettings = IFileStoreExtensions.Combine(null, _container, tenant, "appsettings.json");
            var fileInfo = await _shellsFileStore.GetFileInfoAsync(appSettings);

            if (fileInfo == null && _blobOptions.MigrateFromFiles)
            {
                if (await TryMigrateFromFileAsync(tenant, appSettings))
                {
                    fileInfo = await _shellsFileStore.GetFileInfoAsync(appSettings);
                }
            }
            if (fileInfo != null)
            {
                var stream = await _shellsFileStore.GetFileStreamAsync(appSettings);
                builder.AddJsonStream(stream);
            }
        }

        public async Task SaveAsync(string tenant, IDictionary<string, string> data)
        {
            var appsettings = IFileStoreExtensions.Combine(null, _container, tenant, "appsettings.json");

            JObject config;
            var fileInfo = await _shellsFileStore.GetFileInfoAsync(appsettings);

            if (fileInfo != null)
            {
                using (var stream = await _shellsFileStore.GetFileStreamAsync(appsettings))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        using (var reader = new JsonTextReader(streamReader))
                        {
                            config = await JObject.LoadAsync(reader);
                        }
                    }
                }
            }
            else
            {
                config = new JObject();
            }

            foreach (var key in data.Keys)
            {
                if (data[key] != null)
                {
                    config[key] = data[key];
                }
                else
                {
                    config.Remove(key);
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    using (var jsonWriter = new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented })
                    {
                        await config.WriteToAsync(jsonWriter);
                        await jsonWriter.FlushAsync();
                        memoryStream.Position = 0;
                        await _shellsFileStore.CreateFileFromStreamAsync(appsettings, memoryStream);
                    }
                }
            }

        }

        private async Task<bool> TryMigrateFromFileAsync(string tenant, string destFile)
        {
            var tenantFile = Path.Combine(_fileStoreContainer, tenant, "appsettings.json");
            if (!File.Exists(tenantFile))
            {
                return false;
            }

            using (var file = File.OpenRead(tenantFile))
            {
                await _shellsFileStore.CreateFileFromStreamAsync(destFile, file);
            }

            return true;
        }

        public Task DeleteAsync(string tenant)
        {
            // Todo: Delete Blob Shell Configuration
            return Task.CompletedTask;
        }
    }
}
