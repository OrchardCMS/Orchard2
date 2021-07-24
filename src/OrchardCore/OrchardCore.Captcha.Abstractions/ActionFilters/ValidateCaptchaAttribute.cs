using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Captcha.Services;

namespace OrchardCore.Captcha.ActionFilters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateCaptchaAttribute : ActionFilterAttribute
    {
        private readonly CaptchaMode _mode;

        public ValidateCaptchaAttribute(CaptchaMode mode = CaptchaMode.AlwaysShow)
        {
            _mode = mode;
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var captchaService = context.HttpContext.RequestServices.GetService<CaptchaService>();
            var isValidCaptcha = false; 
            var isRobot = false;

            switch (_mode)
            {
                case CaptchaMode.PreventRobots:
                    isRobot = captchaService.IsThisARobot();
                    break;
                case CaptchaMode.AlwaysShow:
                    isRobot = true;
                    break;
            }

            if (isRobot)
            { 
                isValidCaptcha= await captchaService.ValidateCaptchaAsync((key, error) =>
                {
                    context.ModelState.AddModelError(key, error);
                });               
            }

            await next();

            if (isValidCaptcha && context.ModelState.IsValid)
            {
                captchaService.ThisIsAHuman();
            }
            else
            {
                captchaService.MaybeThisIsARobot();
            }

            return;
        }
    }
}
