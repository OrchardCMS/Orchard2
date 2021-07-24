using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Alias.Models;
using OrchardCore.Alias.Settings;
using OrchardCore.Alias.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using YesSql;

namespace OrchardCore.Alias.Drivers
{
    public class AliasPartDisplayDriver : ContentPartDisplayDriver<AliasPart>
    {

        private readonly ISession _session;
        private readonly IStringLocalizer S;

        public AliasPartDisplayDriver(
            ISession session,
            IStringLocalizer<AliasPartDisplayDriver> localizer
        )
        {
            _session = session;
            S = localizer;
        }

        public override IDisplayResult Display(AliasPart part, BuildPartDisplayContext context)
        {
            var contentType = EncodeAlternateElement(part.ContentItem.ContentType);
            var alias = FormatName(part.Alias);
            context.Shape.Metadata.Alternates.Add($"Content__{contentType}__{alias}");
            return base.Display(part, context);
        }

        public override IDisplayResult Edit(AliasPart aliasPart, BuildPartEditorContext context)
        {
            return Initialize<AliasPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, aliasPart, context.TypePartDefinition.GetSettings<AliasPartSettings>()));
        }

        public override async Task<IDisplayResult> UpdateAsync(AliasPart model, IUpdateModel updater, UpdatePartEditorContext context)
        {
            await updater.TryUpdateModelAsync(model, Prefix, t => t.Alias);

            await foreach (var item in model.ValidateAsync(S, _session))
            {
                updater.ModelState.BindValidationResult(Prefix, item);
            }

            return Edit(model, context);
        }

        private void BuildViewModel(AliasPartViewModel model, AliasPart part, AliasPartSettings settings)
        {
            model.Alias = part.Alias;
            model.AliasPart = part;
            model.ContentItem = part.ContentItem;
            model.Settings = settings;
        }

        private string EncodeAlternateElement(string alternateElement)
        {
            return alternateElement.Replace("-", "__").Replace('.', '_');
        }

        private static string FormatName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            name = name.Trim();
            var nextIsUpper = true;
            var result = new StringBuilder(name.Length);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];

                if (c == '-' || char.IsWhiteSpace(c))
                {
                    nextIsUpper = true;
                    continue;
                }

                if (nextIsUpper)
                {
                    result.Append(c.ToString().ToUpper());
                }
                else
                {
                    result.Append(c);
                }

                nextIsUpper = false;
            }

            return result.ToString();
        }
    }
}
