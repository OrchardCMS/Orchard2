using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using OrchardCore.ReCaptcha.ActionFilters;
using OrchardCore.ReCaptcha.Services;

namespace OrchardCore.ReCaptcha.TagHelpers
{
    [HtmlTargetElement("captcha", TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("captcha", Attributes = "mode,language", TagStructure = TagStructure.WithoutEndTag)]
    public class ReCaptchaTagHelper : TagHelper
    {
        private readonly ReCaptchaService _reCaptchaService;

        public ReCaptchaTagHelper(ReCaptchaService reCaptchaService)
        {
            _reCaptchaService = reCaptchaService;

        }

        [HtmlAttributeName("mode")]
        public ReCaptchaMode Mode { get; set; }

        /// <summary>
        /// The two letter ISO code of the language the captcha should be displayed in
        /// When left blank it will fall back to the default OrchardCore language
        /// </summary>
        [HtmlAttributeName("language")]
        public string Language { get; set; }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            void RenderDivToTagHelper(TagBuilder builder)
            {
                output.TagName = builder.TagName;
                output.MergeAttributes(builder);
                output.TagMode = TagMode.StartTagAndEndTag;
            }

            void DoNotRenderTagHelper()
            {
                output.SuppressOutput();
            }

            _reCaptchaService.ShowCaptchaOrCallCalback(Mode, Language, RenderDivToTagHelper, DoNotRenderTagHelper);

            return Task.CompletedTask;
        }
    }
}
