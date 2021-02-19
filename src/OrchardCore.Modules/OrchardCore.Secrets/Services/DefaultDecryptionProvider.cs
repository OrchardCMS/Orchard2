using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.Services
{
    public class DefaultDecryptionProvider : IDecryptionProvider
    {
        private readonly ISecretService<RsaSecret> _rsaSecretService;

        public DefaultDecryptionProvider(ISecretService<RsaSecret> rsaSecretService)
        {
            _rsaSecretService = rsaSecretService;
        }

        public async Task<IDecryptor> CreateAsync(string protectedData)
        {
            var bytes = Convert.FromBase64String(protectedData);
            var decoded = Encoding.UTF8.GetString(bytes);

            var descriptor = JsonConvert.DeserializeObject<HybridKeyDescriptor>(decoded);

            var encryptionSecret = await _rsaSecretService.GetSecretAsync(descriptor.EncryptionSecretName);
            if (encryptionSecret == null)
            {
                throw new InvalidOperationException($"{descriptor.EncryptionSecretName} secret not found");
            }

            var signingSecret = await _rsaSecretService.GetSecretAsync(descriptor.SigningSecretName);
            if (signingSecret == null)
            {
                throw new InvalidOperationException("Secret not found " + descriptor.SigningSecretName);
            }

            return new DefaultDecryptor(encryptionSecret.PrivateKeyAsBytes(), signingSecret.PublicKeyAsBytes());
        }
    }
}
