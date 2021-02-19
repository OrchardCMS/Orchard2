using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.Services
{
    public class DefaultDecryptor : IDecryptor
    {
        private readonly byte[] _decryptionPrivateKey;
        private readonly byte[] _signingPublicKey;

        public DefaultDecryptor(byte[] decryptionPrivateKey, byte[] signingPublicKey)
        {
            _decryptionPrivateKey = decryptionPrivateKey;
            _signingPublicKey = signingPublicKey;
        }

        public string Decrypt(string protectedData)
        {
            var bytes = Convert.FromBase64String(protectedData);
            var decoded = Encoding.UTF8.GetString(bytes);

            var descriptor = JsonConvert.DeserializeObject<HybridKeyDescriptor>(decoded);

            var protectedBytes = Convert.FromBase64String(descriptor.ProtectedData);
            var signatureBytes = Convert.FromBase64String(descriptor.Signature);

            // Check signature first.
            using var rsaSigner = RsaHelper.GenerateRsaSecurityKey(2048);
            rsaSigner.ImportRSAPublicKey(_signingPublicKey, out _);
            if (!rsaSigner.VerifyData(protectedBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                throw new CryptographicException("Could not verify signature");
            }

            // Decrypt.
            using var rsaDecryptor = RsaHelper.GenerateRsaSecurityKey(2048);
            rsaDecryptor.ImportRSAPrivateKey(_decryptionPrivateKey, out _);

            var aesKey = rsaDecryptor.Decrypt(Convert.FromBase64String(descriptor.Key), RSAEncryptionPadding.Pkcs1);

            using var aes = Aes.Create();
            using var decryptor = aes.CreateDecryptor(aesKey, Convert.FromBase64String(descriptor.Iv));
            using var msDecrypt = new MemoryStream(protectedBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            var plaintext = srDecrypt.ReadToEnd();

            return plaintext;
        }
    }
}
