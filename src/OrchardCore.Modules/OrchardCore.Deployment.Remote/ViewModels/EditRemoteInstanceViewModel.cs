namespace OrchardCore.Deployment.Remote.ViewModels
{
    public class EditRemoteInstanceViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientName { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeySecret { get; set; }
        public string Url { get; set; }
        public string RsaEncryptionSecret { get; set; }
        public string RsaSigningSecret { get; set; }
    }
}
