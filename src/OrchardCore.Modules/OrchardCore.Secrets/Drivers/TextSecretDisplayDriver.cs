using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Secrets.ViewModels;

namespace OrchardCore.Secrets.Drivers
{
    public class TestSecretDisplayDriver : DisplayDriver<Secret, TextSecret>
    {
        private readonly IStringLocalizer S;

        public TestSecretDisplayDriver(IStringLocalizer<TestSecretDisplayDriver> stringLocalizer)
        {
            S = stringLocalizer;
        }

        public override IDisplayResult Display(TextSecret secret)
        {
            return Combine(
                View("TextSecret_Fields_Summary", secret).Location("Summary", "Content"),
                View("TextSecret_Fields_Thumbnail", secret).Location("Thumbnail", "Content"));
        }

        public override Task<IDisplayResult> EditAsync(TextSecret secret, BuildEditorContext context)
        {
            return Task.FromResult<IDisplayResult>(Initialize<TextSecretViewModel>("TextSecret_Fields_Edit", model =>
            {
                // The text value is never returned to the view.
                model.Text = String.Empty;
                model.Context = context;
            }).Location("Content"));
        }

        public override async Task<IDisplayResult> UpdateAsync(TextSecret secret, UpdateEditorContext context)
        {
            var model = new TextSecretViewModel();

            if (await context.Updater.TryUpdateModelAsync(model, Prefix))
            {
                if (context.IsNew && String.IsNullOrEmpty(model.Text))
                {
                    context.Updater.ModelState.AddModelError(Prefix, nameof(model.Text), S["The text value is required"]);
                }

                // The text value is only updated when a new value has been provided.
                if (!String.IsNullOrEmpty(model.Text))
                {
                    secret.Text = model.Text;
                }
            }

            return await EditAsync(secret, context);
        }
    }
}
