using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Localization;
using OrchardCore.Modules;
using OrchardCore.Captcha.ActionFilters;
using OrchardCore.Captcha.ActionFilters.Detection;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ResourceManagement;
using OrchardCore.Captcha.TagHelpers;

namespace OrchardCore.ReCaptcha.TagHelpers
{
    [HtmlTargetElement("captcha", TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("captcha", Attributes = "mode,language", TagStructure = TagStructure.WithoutEndTag)]
    public class ReCaptchaTagHelper : CaptchaTagHelper
    {
        private readonly IResourceManager _resourceManager;
        private readonly ReCaptchaSettings _settings;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IStringLocalizer S;

        public ReCaptchaTagHelper(IOptions<ReCaptchaSettings> optionsAccessor, IResourceManager resourceManager, ILocalizationService localizationService, IHttpContextAccessor httpContextAccessor, ILogger<ReCaptchaTagHelper> logger, IStringLocalizer<ReCaptchaTagHelper> localizer)
         :base(optionsAccessor, httpContextAccessor, logger)
        {
            _resourceManager = resourceManager;
            _settings = optionsAccessor.Value;
            Mode = CaptchaMode.PreventRobots;
            _logger = logger;
            _localizationService = localizationService;
            S = localizer;
        }

        [HtmlAttributeName("mode")]
        public override CaptchaMode Mode { get; set; }

        /// <summary>
        /// The two letter ISO code of the language the captcha should be displayed in
        /// When left blank it will fall back to the default OrchardCore language
        /// </summary>
        [HtmlAttributeName("language")]
        public string Language { get; set; }

        protected override async Task ShowCaptcha(TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "g-recaptcha");
            output.Attributes.SetAttribute("data-sitekey", _settings.SiteKey);
            output.TagMode = TagMode.StartTagAndEndTag;

            var builder = new TagBuilder("script");
            var cultureInfo = await GetCultureAsync();

            var settingsUrl = $"{_settings.ReCaptchaScriptUri}?hl={cultureInfo.TwoLetterISOLanguageName}";

            builder.Attributes.Add("src", settingsUrl);
            _resourceManager.RegisterFootScript(builder);
        }

        private async Task<CultureInfo> GetCultureAsync()
        {
            var language = Language;
            CultureInfo culture = null;

            if (string.IsNullOrWhiteSpace(language))
                language = await _localizationService.GetDefaultCultureAsync();

            try
            {
                culture = CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                _logger.LogWarning(S["Language with name {0} not found", language]);
            }

            return culture;
        }
    }
}
