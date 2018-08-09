namespace OrchardCore.ReCaptcha.Core.Configuration
{
    public class ReCaptchaSettings
    {
        public string SiteKey { get; set; }
        
        public string SecretKey { get; set; }

        public string ReCaptchaScriptUri { get; set; } = Constants.ReCaptchaScriptUri;

        public string ReCaptchaApiUri { get; set; } = Constants.ReCaptchaApiUri;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }
    }
}
