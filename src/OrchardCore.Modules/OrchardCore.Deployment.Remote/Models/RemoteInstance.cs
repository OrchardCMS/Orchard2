namespace OrchardCore.Deployment.Remote.Models
{
    public class RemoteInstance
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientName { get; set; }
        public string Url { get; set; }
        public string ApiKey { get; set; }

        /// <summary>
        /// The name of the api key secret that will be used to authentication this remote instance.
        /// When a secret key is provided, it overrides the <see cref="ApiKey"/> value.
        /// </summary>
        public string ApiKeySecret { get; set; }

        /// <summary>
        /// The name of the rsa secret that can be used to encrypt exported data.
        /// </summary>
        public string RsaEncryptionSecret { get; set; }

            /// <summary>
        /// The name of the rsa secret that can be used to sign encrypted exported data.
        /// </summary>
        public string RsaSigningSecret { get; set; }
    }
}
