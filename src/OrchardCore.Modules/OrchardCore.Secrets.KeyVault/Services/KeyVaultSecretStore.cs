using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.KeyVault.Services
{
    public class KeyVaultSecretStore : ISecretStore
    {
        private readonly KeyVaultClientService _keyVaultClientService;
        private readonly IStringLocalizer S;

        public KeyVaultSecretStore(
            KeyVaultClientService keyVaultClientService,
            IStringLocalizer<KeyVaultSecretStore> stringLocalizer)
        {
            _keyVaultClientService = keyVaultClientService;
            S = stringLocalizer;

        }

        public string Name => nameof(KeyVaultSecretStore);
        public string DisplayName => S["KeyVault Secrets Store"];
        public bool IsReadOnly => false;

        public async  Task<Secret> GetSecretAsync(string key, Type type)
        {
            if (!typeof(Secret).IsAssignableFrom(type))
            {
                throw new ArgumentException("The type must implement " + nameof(Secret));
            }

            var value = await _keyVaultClientService.GetSecretAsync(key);

            if (!String.IsNullOrEmpty(value))
            {
                return JsonConvert.DeserializeObject(value, type) as Secret;
            }

            return null;
        }

        public Task UpdateSecretAsync(string key, Secret secret)
        {
            var value = JsonConvert.SerializeObject(secret);

            return _keyVaultClientService.SetSecretAsync(key, value);
        }

        public Task RemoveSecretAsync(string key)
        {
            return _keyVaultClientService.RemoveSecretAsync(key);
        }
    }
}
