using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Deployment.Remote.Models;
using OrchardCore.Documents;

namespace OrchardCore.Deployment.Remote.Services
{
    public class RemoteInstanceService
    {
        private readonly IDocumentManager<RemoteInstanceList> _documentManager;

        public RemoteInstanceService(IDocumentManager<RemoteInstanceList> documentManager) => _documentManager = documentManager;

        /// <summary>
        /// Loads the remote instances document from the store for updating and that should not be cached.
        /// </summary>
        public Task<RemoteInstanceList> LoadRemoteInstanceListAsync() => _documentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Gets the remote instances document from the cache for sharing and that should not be updated.
        /// </summary>
        public Task<RemoteInstanceList> GetRemoteInstanceListAsync() => _documentManager.GetOrCreateImmutableAsync();

        public async Task<RemoteInstance> LoadRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();
            return FindRemoteInstance(remoteInstanceList, id);
        }

        public async Task<RemoteInstance> GetRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await GetRemoteInstanceListAsync();
            return FindRemoteInstance(remoteInstanceList, id);
        }

        public async Task DeleteRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();
            var remoteInstance = FindRemoteInstance(remoteInstanceList, id);

            if (remoteInstance != null)
            {
                remoteInstanceList.RemoteInstances.Remove(remoteInstance);
                await _documentManager.UpdateAsync(remoteInstanceList);
            }
        }

        public async Task CreateRemoteInstanceAsync(string name, string url, string clientName, string apiKey, string apiKeySecret, string rsaEncryptionSecret, string rsaSigningSecret)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();

            remoteInstanceList.RemoteInstances.Add(new RemoteInstance
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = name,
                Url = url,
                ClientName = clientName,
                ApiKey = apiKey,
                ApiKeySecret = apiKeySecret,
                RsaEncryptionSecret = rsaEncryptionSecret,
                RsaSigningSecret = rsaSigningSecret
            });

            await _documentManager.UpdateAsync(remoteInstanceList);
        }

        public async Task UpdateRemoteInstance(string id, string name, string url, string clientName, string apiKey, string apiKeySecret, string rsaEncryptionSecret, string rsaSigningSecret)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();
            var remoteInstance = FindRemoteInstance(remoteInstanceList, id);

            remoteInstance.Name = name;
            remoteInstance.Url = url;
            remoteInstance.ClientName = clientName;
            remoteInstance.ApiKey = apiKey;
            remoteInstance.ApiKeySecret = apiKeySecret;
            remoteInstance.RsaEncryptionSecret = rsaEncryptionSecret;
            remoteInstance.RsaSigningSecret = rsaSigningSecret;

            await _documentManager.UpdateAsync(remoteInstanceList);
        }

        private static RemoteInstance FindRemoteInstance(RemoteInstanceList remoteInstanceList, string id) =>
            remoteInstanceList.RemoteInstances.FirstOrDefault(x => String.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
