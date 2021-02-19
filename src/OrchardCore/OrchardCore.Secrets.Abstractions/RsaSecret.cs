using System;

namespace OrchardCore.Secrets
{
    public enum RsaSecretType
    {
        PublicPrivatePair,
        Public
    }
    
    public class RsaSecret : Secret
    {
        public RsaSecretType KeyType { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }

    public static class RsaSecretExtensions
    {
        public static byte[] PublicKeyAsBytes(this RsaSecret rsaSecret)
        {
            if (!String.IsNullOrEmpty(rsaSecret.PublicKey))
            {
                return Convert.FromBase64String(rsaSecret.PublicKey);
            }

            return Array.Empty<byte>();
        }

        public static byte[] PrivateKeyAsBytes(this RsaSecret rsaSecret)
        {
            if (!String.IsNullOrEmpty(rsaSecret.PrivateKey))
            {
                return Convert.FromBase64String(rsaSecret.PrivateKey);
            }

            return Array.Empty<byte>();
        }
    }
}
