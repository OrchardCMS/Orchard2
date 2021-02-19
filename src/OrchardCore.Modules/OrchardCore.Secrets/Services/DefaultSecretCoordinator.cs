using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.Secrets.Services
{
    public class DefaultSecretCoordinator : ISecretCoordinator
    {
        private readonly SecretBindingsManager _secretBindingsManager;
        private readonly IEnumerable<ISecretStore> _secretStores;

        public DefaultSecretCoordinator(
            SecretBindingsManager secretBindingsManager,
            IEnumerable<ISecretStore> secretStores)
        {
            _secretBindingsManager = secretBindingsManager;
            _secretStores = secretStores;
        }

        public async Task<IDictionary<string, SecretBinding>> GetSecretBindingsAsync()
        {
            var secretsDocument = await _secretBindingsManager.GetSecretBindingsDocumentAsync();
            return secretsDocument.SecretBindings;
        }

        public async Task<IDictionary<string, SecretBinding>> LoadSecretBindingsAsync()
        {
            var secretsDocument = await _secretBindingsManager.LoadSecretBindingsDocumentAsync();
            return secretsDocument.SecretBindings;
        }

        public async Task UpdateSecretAsync(string key, SecretBinding secretBinding, Secret secret)
        {
            if (!String.Equals(key, key.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The name contains invalid characters");
            }

            var secretStore = _secretStores.FirstOrDefault(x => String.Equals(x.Name, secretBinding.Store, StringComparison.OrdinalIgnoreCase));
            if (secretStore != null)
            {
                await _secretBindingsManager.UpdateSecretBindingAsync(key, secretBinding);
                // This is a noop rather than an exception as updating a readonly store is considered a noop.
                if (!secretStore.IsReadOnly)
                {
                    await secretStore.UpdateSecretAsync(key, secret);
                }
            }
            else
            {
                throw new InvalidOperationException("The specified store was not found " + secretBinding.Store);
            }
        }

        public async Task RemoveSecretAsync(string key, string store)
        {
            var secretStore = _secretStores.FirstOrDefault(x => String.Equals(x.Name, store, StringComparison.OrdinalIgnoreCase));
            if (secretStore != null)
            {
                await _secretBindingsManager.RemoveSecretBindingAsync(key);
                // This is a noop rather than an exception as updating a readonly store is considered a noop.
                if (!secretStore.IsReadOnly)
                {
                    await secretStore.RemoveSecretAsync(key);
                }
            }
            else
            {
                throw new InvalidOperationException("The specified store was not found " + store);
            }
        }

        public async Task<Secret> GetSecretAsync(string key, Type type)
        {
            if (!typeof(Secret).IsAssignableFrom(type))
            {
                throw new ArgumentException("The type must implement " + nameof(Secret));
            }

            // TODO this has to find the binding first, to know which store it is in.
            var bindings = await GetSecretBindingsAsync();
            var binding = bindings[key];
            if (binding == null)
            {
                return null;
            }

            var secretStore = _secretStores.FirstOrDefault(x => String.Equals(x.Name, binding.Store, StringComparison.OrdinalIgnoreCase));

            if (secretStore == null)
            {
                return null;
            }

            var secret = await secretStore.GetSecretAsync(key, type);

            return secret;
        }

        public IEnumerator<SecretStoreDescriptor> GetEnumerator()
        {
            return _secretStores.Select(x => new SecretStoreDescriptor { Name = x.Name, IsReadOnly = x.IsReadOnly, DisplayName = x.DisplayName }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
