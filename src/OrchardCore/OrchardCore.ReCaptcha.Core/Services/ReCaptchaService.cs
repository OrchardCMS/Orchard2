using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.ReCaptcha.ActionFilters;
using OrchardCore.ReCaptcha.ActionFilters.Detection;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ResourceManagement;

namespace OrchardCore.ReCaptcha.Services
{
    public class ReCaptchaService
    {
        private readonly ReCaptchaClient _reCaptchaClient;
        private readonly ReCaptchaSettings _settings;
        private readonly IEnumerable<IDetectRobots> _robotDetectors;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IResourceManager _resourceManager;
        private readonly IStringLocalizer S;

        public ReCaptchaService(ReCaptchaClient reCaptchaClient,
            IOptions<ReCaptchaSettings> optionsAccessor,
            IEnumerable<IDetectRobots> robotDetectors,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ReCaptchaService> logger,
            IResourceManager resourceManager,
            IStringLocalizer<ReCaptchaService> stringLocalizer)
        {
            _reCaptchaClient = reCaptchaClient;
            _settings = optionsAccessor.Value;
            _resourceManager = resourceManager;
            _robotDetectors = robotDetectors;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            S = stringLocalizer;
        }

        internal void ShowCaptchaOrCallCalback(ReCaptchaMode mode, string language, Action<TagBuilder> useDiv, Action otherwise)
        {
            var robotDetected = _robotDetectors.Invoke(d => d.DetectRobot(), _logger).Any(d => d.IsRobot) && mode == ReCaptchaMode.PreventRobots;
            var alwaysShow = mode == ReCaptchaMode.AlwaysShow;
            var isConfigured = _settings != null;

            if (isConfigured && (robotDetected || alwaysShow))
            {
                var divBuilder = BuildDiv();
                useDiv(divBuilder);

                var cultureInfo = GetCulture(language);
                RegisterReCaptchaScript(cultureInfo);
            }
            else
            {
                otherwise();
            }
        }

        private TagBuilder BuildDiv()
        {
            var builder = new TagBuilder("div");
            builder.Attributes.Add("class", "g-recaptcha");
            builder.Attributes.Add("data-sitekey", _settings.SiteKey);

            return builder;
        }

        private void RegisterReCaptchaScript(CultureInfo cultureInfo)
        {
            var scriptBuilder = new TagBuilder("script");
            var settingsUrl = $"{_settings.ReCaptchaScriptUri}?hl={cultureInfo.TwoLetterISOLanguageName}";
            scriptBuilder.Attributes.Add("src", settingsUrl);
            _resourceManager.RegisterFootScript(scriptBuilder);
        }

        private CultureInfo GetCulture(string language)
        {
            CultureInfo culture = null;

            if (string.IsNullOrWhiteSpace(language))
            {
                return Thread.CurrentThread.CurrentCulture;
            }

            try
            {
                culture = CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                _logger.LogWarning("Language with name {Language} not found", language);
            }

            return culture;
        }
        /// <summary>
        /// Flags the behavior as that of a robot
        /// </summary>
        public void MaybeThisIsARobot()
        {
            _robotDetectors.Invoke(i => i.FlagAsRobot(), _logger);
        }

        /// <summary>
        /// Determines if the request has been made by a robot
        /// </summary>
        /// <returns>Yes (true) or no (false)</returns>
        public bool IsThisARobot()
        {
            var result = _robotDetectors.Invoke(i => i.DetectRobot(), _logger);
            return result.Any(a => a.IsRobot);
        }

        /// <summary>
        /// Clears all robot markers, we are dealing with a human
        /// </summary>
        /// <returns></returns>
        public void ThisIsAHuman()
        {
            _robotDetectors.Invoke(i => i.IsNotARobot(), _logger);
        }

        /// <summary>
        /// Verifies the ReCaptcha response with the ReCaptcha webservice
        /// </summary>
        /// <param name="reCaptchaResponse"></param>
        /// <returns></returns>
        public async Task<bool> VerifyCaptchaResponseAsync(string reCaptchaResponse)
        {
            return !String.IsNullOrWhiteSpace(reCaptchaResponse) && await _reCaptchaClient.VerifyAsync(reCaptchaResponse, _settings.SecretKey);
        }

        /// <summary>
        /// Validates the captcha that is in the Form of the current request
        /// </summary>
        /// <param name="reportError">Lambda for reporting errors</param>
        public async Task<bool> ValidateCaptchaAsync(Action<string, string> reportError)
        {
            var reCaptchaResponse = _httpContextAccessor.HttpContext?.Request?.Form?[Constants.ReCaptchaServerResponseHeaderName].ToString();

            var isValid = !String.IsNullOrEmpty(reCaptchaResponse) && await VerifyCaptchaResponseAsync(reCaptchaResponse);

            if (!isValid)
            {
                reportError("ReCaptcha", S["Failed to validate captcha"]);
            }

            return isValid;
        }
    }
}
