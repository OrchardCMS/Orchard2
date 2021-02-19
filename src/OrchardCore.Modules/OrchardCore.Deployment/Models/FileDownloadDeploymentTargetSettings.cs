namespace OrchardCore.Deployment.Models
{
    public class FileDownloadDeploymentTargetSettings
    {
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
