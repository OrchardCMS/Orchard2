using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.Services
{
    public class DefaultEncryptionProvider : IEncryptionProvider
    {
        private readonly ISecretService<RsaSecret> _rsaSecretService;

        public DefaultEncryptionProvider(ISecretService<RsaSecret> rsaSecretService)
        {
            _rsaSecretService = rsaSecretService;
        }

        public async Task<IEncryptor> CreateAsync(string encryptionSecretName, string signingSecretName)
        {
            var encryptionSecret = await _rsaSecretService.GetSecretAsync(encryptionSecretName);
            if (encryptionSecret == null)
            {
                throw new InvalidOperationException("Secret not found " + encryptionSecretName);
            }

            var signingSecret = await _rsaSecretService.GetSecretAsync(signingSecretName);
            if (signingSecret == null)
            {
                throw new InvalidOperationException("Secret not found " + signingSecretName);
            }

            // This becomes irrelevent as we now need the private key for the signature.

            if (signingSecret.KeyType != RsaSecretType.PublicPrivatePair)
            {
                throw new InvalidOperationException("Secret cannot be used for signing");
            }

            // When encrypting, you use their public key to write a message and they use their private key to read it.
            // When signing, you use your private key to write message's signature, and they use your public key to check if it's really yours.

            return new DefaultEncryptor(encryptionSecret.PublicKeyAsBytes(), signingSecret.PrivateKeyAsBytes(), encryptionSecretName, signingSecretName);
        }
    }
}
