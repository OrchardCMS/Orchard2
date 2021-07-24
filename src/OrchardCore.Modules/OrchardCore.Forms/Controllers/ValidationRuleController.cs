using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.Forms.Drivers;
using OrchardCore.Forms.Helpers;

namespace OrchardCore.Forms.Controllers
{
    [Route("api/validation")]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    [ApiController]
    public class ValidationApiController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ValidationRuleHelpers _validationRuleHelpers;
        private readonly IStringLocalizer S;
        private readonly ValidationRuleOptions _validationRuleOptions;
        public ValidationApiController(
            IAuthorizationService authorizationService,
            ValidationRuleHelpers validationRuleHelpers,
            IOptions<ValidationRuleOptions> validationRuleOptions,
            IStringLocalizer<ValidationApiController> stringLocalizer)
        {
            _authorizationService = authorizationService;
            _validationRuleHelpers = validationRuleHelpers;
            _validationRuleOptions = validationRuleOptions.Value;
            S = stringLocalizer;
        }
        [HttpPost]
        [Route("ValidateInputByRule")]
        public async Task<IActionResult> ValidateInputByRule()
        {
            var contentItemId = Request.Form["ContentItemId"];
            var formName = Request.Form["FormName"];
            var formValue = Request.Form["FormValue"];
            if (String.IsNullOrEmpty(formValue))
            {
                formValue = String.Empty;
            }
            if (String.IsNullOrWhiteSpace(contentItemId) || String.IsNullOrWhiteSpace(formName))
            {
                return BadRequest(S["contentItemId, formName are required parameters"]);
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ValidationRule))
            {
                return Forbid();
            }

            var validationRuleAspects = await _validationRuleHelpers.GetValidationRuleAspects(contentItemId);
            var validationRuleItem = validationRuleAspects.FirstOrDefault(a => a.FormInputName == formName);
            var validationRuleProvider = _validationRuleOptions.ValidationRuleProviders.FirstOrDefault(a => a.Name.Equals(validationRuleItem.Type,StringComparison.OrdinalIgnoreCase));
            var validationResult = validationRuleProvider.ValidateInputByRuleAsync(validationRuleItem.Option, formValue);
            return Ok(new { result = validationResult });
        }
        [HttpPost]
        [Route("ValidateFormByRule")]
        public async Task<IActionResult> ValidateFormByRule()
        {
            var contentItemId = Request.Form["ContentItemId"];
            var formData = Request.Form["FormData"];
            var formDictionary = formData.ToString().Split('&');
            if (String.IsNullOrWhiteSpace(contentItemId) || String.IsNullOrWhiteSpace(formData))
            {
                return BadRequest(S["contentItemId, formData are required parameters"]);
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ValidationRule))
            {
                return Forbid();
            }

            var validationRuleAspects = await _validationRuleHelpers.GetValidationRuleAspects(contentItemId);
            var errorList = new List<string>();
            foreach (var item in formDictionary)
            {
                var formNameValue = item.Split('=');
                var formName = formNameValue[0];
                var formValue = formNameValue[1];
                var validationRuleItem = validationRuleAspects.FirstOrDefault(a => a.FormInputName == formName);
                var validationRuleProvider = _validationRuleOptions.ValidationRuleProviders.FirstOrDefault(a => a.Name.Equals(validationRuleItem.Type, StringComparison.OrdinalIgnoreCase));
                var validationResult = validationRuleProvider.ValidateInputByRuleAsync(validationRuleItem.Option, formValue);
                if(!validationResult) errorList.Add(formName);
            }
    
            return Ok(new { result = String.Join(",",errorList) });
        }
    }
}
