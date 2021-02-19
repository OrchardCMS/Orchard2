namespace OrchardCore.Deployment.Remote.Models
{
    public class RemoteClient
    {
        public string Id { get; set; }
        public string ClientName { get; set; }
        public byte[] ProtectedApiKey { get; set; }

        /// <summary>
        /// The name of the api key secret that will be used to authentication this remote instance.
        /// When a secret key is provided, it overrides the <see cref="ProtectedApiKey"/> value.
        /// </summary>        
        public string ApiKeySecret { get; set; }
    }
}
