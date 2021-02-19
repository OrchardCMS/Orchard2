namespace OrchardCore.Secrets.Services
{
    internal class HybridKeyDescriptor
    {
        public string EncryptionSecretName { get; set; }
        public string Key { get; set; }
        public string Iv { get; set; }
        public string ProtectedData { get; set; }
        public string Signature { get; set; }
        public string SigningSecretName { get; set; }
    }
}
