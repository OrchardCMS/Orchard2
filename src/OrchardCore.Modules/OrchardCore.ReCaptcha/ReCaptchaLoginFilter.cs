using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Layout;
using OrchardCore.Entities;
using OrchardCore.ReCaptcha.Configuration;
using OrchardCore.ReCaptcha.Services;
using OrchardCore.Settings;

namespace OrchardCore.ReCaptcha
{
    public class ReCaptchaLoginFilter : IAsyncResultFilter
    {
        private readonly ILayoutAccessor _layoutAccessor;
        private readonly ISiteService _siteService;
        private readonly ReCaptchaService _reCaptchaService;
        private readonly IShapeFactory _shapeFactory;

        public ReCaptchaLoginFilter(
            ILayoutAccessor layoutAccessor,
            ISiteService siteService,
            ReCaptchaService reCaptchaService,
            IShapeFactory shapeFactory)
        {
            _layoutAccessor = layoutAccessor;
            _siteService = siteService;
            _reCaptchaService = reCaptchaService;
            _shapeFactory = shapeFactory;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!(context.Result is ViewResult || context.Result is PageResult)
                || !String.Equals("OrchardCore.Users", Convert.ToString(context.RouteData.Values["area"]), StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var settings = (await _siteService.GetSiteSettingsAsync()).As<ReCaptchaSettings>();

            if (!settings.IsValid())
            {
                await next();
                return;
            }

            var layout = await _layoutAccessor.GetLayoutAsync();
            var recaptCha = await _shapeFactory.CreateAsync("ReCaptcha");
            var test = await _shapeFactory.New.ReCaptcha();

            if (layout.Zones["AfterLogin"] is IShape afterLoginZone && _reCaptchaService.IsThisARobot())
            {
                await afterLoginZone.AddAsync(recaptCha);
            }

            if (layout.Zones["AfterForgotPassword"] is IShape afterForgotPasswordZone)
            {
                await afterForgotPasswordZone.AddAsync(recaptCha);
            }

            if (layout.Zones["AfterRegister"] is IShape afterRegisterZone)
            {
                await afterRegisterZone.AddAsync(recaptCha);
            }

            if (layout.Zones["AfterResetPassword"] is IShape afterResetPasswordZone)
            {
                await afterResetPasswordZone.AddAsync(recaptCha);
            }

            await next();
        }
    }
}
