using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Tags;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Liquid.Ast;
using OrchardCore.ReCaptcha.ActionFilters;
using OrchardCore.ReCaptcha.ActionFilters.Detection;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ReCaptcha.Services;

namespace OrchardCore.ReCaptcha.Liquid
{
    public class ReCaptchaTag : ArgumentsTag
    {
        public override async ValueTask<Completion> WriteToAsync(TextWriter writer, TextEncoder encoder, TemplateContext context, FilterArgument[] args)
        {
            if (!context.AmbientValues.TryGetValue("Services", out var servicesValue))
            {
                throw new ArgumentException("Services missing while invoking 'helper'");
            }

            var services = servicesValue as IServiceProvider;

            var reCaptchaService = services.GetRequiredService<ReCaptchaService>();

            var arguments = (FilterArguments)(await new ArgumentsExpression(args).EvaluateAsync(context)).ToObjectValue();
            var mode = arguments.HasNamed("mode") ? Enum.Parse<ReCaptchaMode>(arguments["mode"].ToStringValue()) : ReCaptchaMode.PreventRobots;
            var language = arguments.HasNamed("language") ? arguments["language"].ToStringValue() : null;



            var settings = services.GetRequiredService<IOptions<ReCaptchaSettings>>().Value;
            var robotDetectors = services.GetRequiredService<IEnumerable<IDetectRobots>>();

            void RenderDivToTagHelper(TagBuilder builder)
            {
                builder.WriteTo(writer, (HtmlEncoder)encoder);
            }
            reCaptchaService.ShowCaptchaOrCallCalback(mode, language, RenderDivToTagHelper, () => { });

            return Completion.Normal;
        }
    }
}
