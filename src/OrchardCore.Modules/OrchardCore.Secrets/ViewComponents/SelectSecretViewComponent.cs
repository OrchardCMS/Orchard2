using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.Secrets.ViewModels;

namespace OrchardCore.Secrets.ViewComponents
{
    public class SelectSecretViewComponent : ViewComponent
    {
        private readonly ISecretCoordinator _secretCoordinator;
        private readonly IStringLocalizer S;

        public SelectSecretViewComponent(ISecretCoordinator secretCoordinator, IStringLocalizer<SelectSecretViewComponent> stringLocalizer)
        {
            _secretCoordinator = secretCoordinator;
            S = stringLocalizer;
        }

        public async Task<IViewComponentResult> InvokeAsync(string secretType, string selectedSecret, string htmlName, bool required)
        {
            var secretBindings = await _secretCoordinator.GetSecretBindingsAsync();
            var secrets = secretBindings.Where(x => String.Equals(secretType, x.Value.Type, StringComparison.OrdinalIgnoreCase))
                .Select(x => new SelectListItem() { Text = x.Key, Value = x.Key, Selected = String.Equals(x.Key, selectedSecret, StringComparison.OrdinalIgnoreCase) })
                .ToList();

            if (!required)
            {
                secrets.Insert(0, new SelectListItem() { Text = S["None"], Value = String.Empty });
            }

            var model = new SelectSecretViewModel
            {
                HtmlName = htmlName,
                Secrets = secrets
            };

            return View(model);
        }
    }
}
