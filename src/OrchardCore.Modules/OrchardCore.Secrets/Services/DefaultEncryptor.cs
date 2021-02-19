using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.Services
{
    public class DefaultEncryptor : IEncryptor
    {
        private readonly string _encryptionSecretName;
        private readonly byte[] _encryptionPublicKey;
        private readonly byte[] _signingPrivateKey;
        private readonly string _signingSecretName;

        public DefaultEncryptor(byte[] encryptionPublicKey, byte[] signingPrivateKey, string encryptionSecretName, string signingSecretName)
        {
            _encryptionPublicKey = encryptionPublicKey;
            _signingPrivateKey = signingPrivateKey;
            _encryptionSecretName = encryptionSecretName;
            _signingSecretName = signingSecretName;
        }

        public string Encrypt(string plainText)
        {
            byte[] encrypted;
            using var aes = Aes.Create();
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            using var rsaEncryptor = RsaHelper.GenerateRsaSecurityKey(2048);
            rsaEncryptor.ImportRSAPublicKey(_encryptionPublicKey, out _);

            using var rsaSigner = RsaHelper.GenerateRsaSecurityKey(2048);
            rsaSigner.ImportRSAPrivateKey(_signingPrivateKey, out _);

            var rsaEncryptedAesKey = rsaEncryptor.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);            
            var signature = rsaSigner.SignData(encrypted, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var descriptor = new HybridKeyDescriptor
            {
                EncryptionSecretName = _encryptionSecretName,
                Key = Convert.ToBase64String(rsaEncryptedAesKey),
                Iv = Convert.ToBase64String(aes.IV),
                ProtectedData = Convert.ToBase64String(encrypted),
                Signature = Convert.ToBase64String(signature),
                SigningSecretName = _signingSecretName
            };

            var serialized = JsonConvert.SerializeObject(descriptor);
            var encodedDescriptor = Convert.ToBase64String(Encoding.UTF8.GetBytes(serialized));

            return encodedDescriptor;
        }
    }
}
