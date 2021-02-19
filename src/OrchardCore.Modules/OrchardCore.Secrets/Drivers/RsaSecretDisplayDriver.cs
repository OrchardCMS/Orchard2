using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Secrets.Services;
using OrchardCore.Secrets.ViewModels;

namespace OrchardCore.Secrets.Drivers
{
    public class RsaSecretDisplayDriver : DisplayDriver<Secret, RsaSecret>
    {
        private readonly IStringLocalizer S;

        public RsaSecretDisplayDriver(IStringLocalizer<RsaSecretDisplayDriver> stringLocalizer)
        {
            S = stringLocalizer;
        }

        public override IDisplayResult Display(RsaSecret secret)
        {
            return Combine(
                View("RsaSecret_Fields_Summary", secret).Location("Summary", "Content"),
                View("RsaSecret_Fields_Thumbnail", secret).Location("Thumbnail", "Content"));
        }

        public override Task<IDisplayResult> EditAsync(RsaSecret secret, BuildEditorContext context)
        {
            return Task.FromResult<IDisplayResult>(Initialize<RsaSecretViewModel>("RsaSecret_Fields_Edit", model =>
            {
                // Generate new keys when creating.
                if (context.IsNew)
                {
                    using (var rsa = RsaHelper.GenerateRsaSecurityKey(2048))
                    {
                        if (String.IsNullOrEmpty(secret.PublicKey))
                        {
                            model.PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                        }
                        else
                        {
                            // If key validation fails we return the value supplied.
                            model.PublicKey = secret.PublicKey;
                        }
                        if (String.IsNullOrEmpty(secret.PrivateKey))
                        {
                            model.PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                        }
                        else
                        {
                            model.PrivateKey = secret.PrivateKey;
                        }
                    }
                }
                else
                {
                    // The private key is never returned to the view when editing.
                    model.PublicKey = secret.PublicKey;

                    using (var rsa = RsaHelper.GenerateRsaSecurityKey(2048))
                    {
                        model.NewPublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                        model.NewPrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                    }
                }

                model.KeyType = secret.KeyType;
                model.KeyTypes = new List<SelectListItem>
                {
                    new SelectListItem() { Text = S["Public Key"], Value = ((int)RsaSecretType.Public).ToString(), Selected = model.KeyType == RsaSecretType.Public },
                    new SelectListItem() { Text = S["Public / Private Key Pair"], Value = ((int)RsaSecretType.PublicPrivatePair).ToString(), Selected = model.KeyType == RsaSecretType.PublicPrivatePair }
                };
                model.Context = context;
            }).Location("Content"));
        }

        public override async Task<IDisplayResult> UpdateAsync(RsaSecret secret, UpdateEditorContext context)
        {
            var model = new RsaSecretViewModel();

            if (await context.Updater.TryUpdateModelAsync(model, Prefix))
            {
                secret.KeyType = model.KeyType;

                // The view will contain the private key when creating.
                if (context.IsNew)
                {
                    secret.PublicKey = model.PublicKey;
                    secret.PrivateKey = model.PrivateKey;
                }

                if (model.KeyType == RsaSecretType.Public)
                {
                    secret.PublicKey = model.PublicKey;
                }

                if (model.CycleKey && model.KeyType == RsaSecretType.PublicPrivatePair)
                {
                    secret.PublicKey = model.NewPublicKey;
                    secret.PrivateKey = model.NewPrivateKey;
                }

                if (model.KeyType == RsaSecretType.PublicPrivatePair)
                {
                    try
                    {
                        using (var rsa = RsaHelper.GenerateRsaSecurityKey(2048))
                        {
                            rsa.ImportRSAPrivateKey(secret.PrivateKeyAsBytes(), out _);
                        }
                    }
                    catch (CryptographicException)
                    {
                        if (context.IsNew)
                        {
                            context.Updater.ModelState.AddModelError(Prefix, nameof(model.PrivateKey), S["The private key cannot be decoded."]);
                        }
                        else
                        {
                            context.Updater.ModelState.AddModelError(Prefix, nameof(model.NewPrivateKey), S["The private key cannot be decoded."]);
                        }
                    }
                }

                try
                {
                    using (var rsa = RsaHelper.GenerateRsaSecurityKey(2048))
                    {
                        rsa.ImportRSAPublicKey(secret.PublicKeyAsBytes(), out _);
                    }
                }
                catch (CryptographicException)
                {
                    if (context.IsNew)
                    {
                        context.Updater.ModelState.AddModelError(Prefix, nameof(model.PublicKey), S["The public key cannot be decoded"]);
                    }
                    else
                    {
                        context.Updater.ModelState.AddModelError(Prefix, nameof(model.NewPublicKey), S["The public key cannot be decoded"]);
                    }
                }
            }

            return await EditAsync(secret, context);
        }
    }
}
