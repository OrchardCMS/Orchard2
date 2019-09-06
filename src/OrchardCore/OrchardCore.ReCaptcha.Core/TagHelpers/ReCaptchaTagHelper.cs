using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Localization;
using OrchardCore.Modules;
using OrchardCore.ReCaptcha.ActionFilters;
using OrchardCore.ReCaptcha.ActionFilters.Detection;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ResourceManagement;

namespace OrchardCore.ReCaptcha.TagHelpers
{
    [HtmlTargetElement("captcha", TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("captcha", Attributes = "mode", TagStructure = TagStructure.WithoutEndTag)]
    public class ReCaptchaTagHelper : TagHelper
    {
        private readonly IResourceManager _resourceManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ReCaptchaSettings _settings;
        private readonly ILogger<ReCaptchaTagHelper> _logger;
        private readonly ILocalizationService _localizationService;

        public ReCaptchaTagHelper(IOptions<ReCaptchaSettings> optionsAccessor, IResourceManager resourceManager, ILocalizationService localizationService, IHttpContextAccessor httpContextAccessor, ILogger<ReCaptchaTagHelper> logger)
        {
            _resourceManager = resourceManager;
            _httpContextAccessor = httpContextAccessor;
            _settings = optionsAccessor.Value;
            Mode = ReCaptchaMode.PreventRobots;
            _logger = logger;
            _localizationService = localizationService;
        }

        [HtmlAttributeName("mode")]
        public ReCaptchaMode Mode { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var robotDetectors = _httpContextAccessor.HttpContext.RequestServices.GetServices<IDetectRobots>();
            var robotDetected = robotDetectors.Invoke(d => d.DetectRobot(), _logger).Any(d => d.IsRobot) && Mode == ReCaptchaMode.PreventRobots;
            var alwaysShow = Mode == ReCaptchaMode.AlwaysShow;
            var isConfigured = _settings != null;

            if (isConfigured && (robotDetected || alwaysShow))
            {
                await ShowCaptcha(output);
            }
            else
            {
                output.SuppressOutput();
            }
        }

        private async Task ShowCaptcha(TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "g-recaptcha");
            output.Attributes.SetAttribute("data-sitekey", _settings.SiteKey);
            output.TagMode = TagMode.StartTagAndEndTag;

            var builder = new TagBuilder("script");

            var culture = await _localizationService.GetDefaultCultureAsync();

            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            var settingsUrl = $"{_settings.ReCaptchaScriptUri}?hl={cultureInfo.TwoLetterISOLanguageName}";

            builder.Attributes.Add("src", settingsUrl);
            _resourceManager.RegisterFootScript(builder);
        }
    }
}
