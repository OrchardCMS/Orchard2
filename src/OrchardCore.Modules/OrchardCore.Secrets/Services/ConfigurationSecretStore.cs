using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OrchardCore.Environment.Shell.Configuration;

namespace OrchardCore.Secrets.Services
{
    public class ConfigurationSecretStore : ISecretStore
    {
        private readonly IShellConfiguration _shellConfiguration;

        private readonly IStringLocalizer S;

        public ConfigurationSecretStore(
            IShellConfiguration shellConfiguration,
            IStringLocalizer<DatabaseSecretStore> stringLocalizer)
        {
            _shellConfiguration = shellConfiguration;
            S = stringLocalizer;
        }

        public string Name => nameof(ConfigurationSecretStore);
        public string DisplayName => S["Configuration Secret Store"];
        public bool IsReadOnly => true;

        public Task<Secret> GetSecretAsync(string key, Type type)
        {
            if (!typeof(Secret).IsAssignableFrom(type))
            {
                throw new ArgumentException("The type must implement " + nameof(Secret));
            }    

            return Task.FromResult(_shellConfiguration.GetSection($"OrchardCore_Secrets_ConfigurationSecretStore:{key}").Get(type) as Secret);
        }

        public Task UpdateSecretAsync(string key, Secret secret)
        {
            throw new NotSupportedException("The Configuration Secret Store is ReadOnly");
        }

        public Task RemoveSecretAsync(string key)
        {
            throw new NotSupportedException("The Configuration Secret Store is ReadOnly");
        }
    }
}
