namespace OrchardCore.Captcha.Configuration
{
    public class CaptchaSettings
    {
        /// <summary>
        /// Value for supplying the amount of lenience we are willing to show robots
        /// </summary>
        public int DetectionThreshold { get; set; } = 5;

    }
}
