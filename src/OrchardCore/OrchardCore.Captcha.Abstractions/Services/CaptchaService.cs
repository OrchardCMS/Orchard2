using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.Modules;
using OrchardCore.Captcha.ActionFilters.Detection;

namespace OrchardCore.Captcha.Services
{
    public abstract class CaptchaService
    {
        private readonly IEnumerable<IDetectRobots> _robotDetectors;
        private readonly ILogger _logger;
        public CaptchaService(IEnumerable<IDetectRobots> robotDetectors,ILogger logger)
        {
            _robotDetectors = robotDetectors;
            _logger = logger;
        }

        /// <summary>
        /// Flags the behavior as that of a robot
        /// </summary>
        public virtual void MaybeThisIsARobot()
        {
            _robotDetectors.Invoke(i => i.FlagAsRobot(), _logger);
        }

        /// <summary>
        /// Determines if the request has been made by a robot
        /// </summary>
        /// <returns>Yes (true) or no (false)</returns>
        public virtual bool IsThisARobot()
        {
            var result = _robotDetectors.Invoke(i => i.DetectRobot(), _logger);
            return result.Any(a => a.IsRobot);
        }

        /// <summary>
        /// Clears all robot markers, we are dealing with a human
        /// </summary>
        /// <returns></returns>
        public virtual void ThisIsAHuman()
        {
            _robotDetectors.Invoke(i => i.IsNotARobot(), _logger);
        }

        /// <summary>
        /// Verifies the captcha response
        /// </summary>
        /// <param name="captchaResponse"></param>
        /// <returns></returns>
        public abstract Task<bool> VerifyCaptchaResponseAsync(string captchaResponse);
        
        /// <summary>
        /// Validates the captcha that is in the Form of the current request
        /// </summary>
        /// <param name="reportError">Lambda for reporting errors</param>
        public abstract Task<bool> ValidateCaptchaAsync(Action<string, string> reportError);
        
    }
}
