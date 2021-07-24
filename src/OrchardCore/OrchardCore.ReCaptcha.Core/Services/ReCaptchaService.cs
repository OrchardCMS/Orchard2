using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Captcha.ActionFilters.Detection;
using OrchardCore.Captcha.Services;
using OrchardCore.ReCaptcha.Configuration;

namespace OrchardCore.ReCaptcha.Services
{
    public class ReCaptchaService : CaptchaService
    {
        private readonly ReCaptchaClient _reCaptchaClient;
        private readonly ReCaptchaSettings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer S;

        public ReCaptchaService(ReCaptchaClient reCaptchaClient, IOptions<ReCaptchaSettings> optionsAccessor, IEnumerable<IDetectRobots> robotDetectors, IHttpContextAccessor httpContextAccessor, ILogger<ReCaptchaService> logger, IStringLocalizer<ReCaptchaService> stringLocalizer)
         :base(robotDetectors,logger)
        {
            _reCaptchaClient = reCaptchaClient;
            _settings = optionsAccessor.Value;
            _httpContextAccessor = httpContextAccessor;
            S = stringLocalizer;
        }

        /// <summary>
        /// Verifies the ReCaptcha response with the ReCaptcha webservice
        /// </summary>
        /// <param name="reCaptchaResponse"></param>
        /// <returns></returns>
        public override async Task<bool> VerifyCaptchaResponseAsync(string reCaptchaResponse)
        {
            return !String.IsNullOrWhiteSpace(reCaptchaResponse) && await _reCaptchaClient.VerifyAsync(reCaptchaResponse, _settings.SecretKey);
        }

        /// <summary>
        /// Validates the captcha that is in the Form of the current request
        /// </summary>
        /// <param name="reportError">Lambda for reporting errors</param>
        public override async Task<bool> ValidateCaptchaAsync(Action<string, string> reportError)
        {
            if (!_settings.IsValid())
            {
                _logger.LogWarning("The ReCaptcha settings are not valid");
                return false;
            }

            // We use the header value as default if it's passed
            var reCaptchaResponse = _httpContextAccessor.HttpContext?.Request.Headers[Constants.ReCaptchaServerResponseHeaderName];

            // If this is a standard form post we get the token from the form values if not affected previously in the header
            if(String.IsNullOrEmpty(reCaptchaResponse) && (_httpContextAccessor.HttpContext?.Request.HasFormContentType ?? false))
            {
                reCaptchaResponse = _httpContextAccessor.HttpContext.Request.Form[Constants.ReCaptchaServerResponseHeaderName].ToString();
            }

            var isValid = !String.IsNullOrEmpty(reCaptchaResponse) && await VerifyCaptchaResponseAsync(reCaptchaResponse);

            if (!isValid)
            {
                reportError("ReCaptcha", S["Failed to validate captcha"]);
            }

            return isValid;
        }
    }
}
