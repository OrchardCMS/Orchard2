using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Captcha.ActionFilters;
using OrchardCore.Captcha.ActionFilters.Detection;
using OrchardCore.Captcha.Configuration;

namespace OrchardCore.Captcha.TagHelpers
{
    public abstract class CaptchaTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CaptchaSettings _settings;
        private readonly ILogger _logger;

        public CaptchaTagHelper(IOptions<CaptchaSettings> optionsAccessor, IHttpContextAccessor httpContextAccessor, ILogger logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = optionsAccessor.Value;
            Mode = CaptchaMode.PreventRobots;
            _logger = logger;
        }

        public virtual CaptchaMode Mode { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var captchaRequired = false;
            var isConfigured = _settings != null;
            
            if(isConfigured)
            {
                if (Mode == CaptchaMode.AlwaysShow)
                {
                    captchaRequired = true;
                }
                else if (Mode == CaptchaMode.PreventRobots)
                {
                    var robotDetectors = _httpContextAccessor.HttpContext.RequestServices.GetServices<IDetectRobots>();
                    var robotDetected = robotDetectors.Invoke(d => d.DetectRobot(), _logger).Any(d => d.IsRobot);
                
                    if (robotDetected)
                    {
                        captchaRequired = true;
                    }
                }
            }

            if (captchaRequired)
            {
                await ShowCaptcha(output);
            }
            else
            {
                output.SuppressOutput();
            }           
            
        }

        protected abstract Task ShowCaptcha(TagHelperOutput output);
        
    }
}
