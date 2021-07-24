using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Forms.Models;

namespace OrchardCore.Forms.Handlers
{
    public class ValidationRulePartContentHandler : ContentHandlerBase
    {
        public override Task GetContentItemAspectAsync(ContentItemAspectContext context)
        {
            return context.ForAsync<List<ValidationRuleAspect>>(aspect =>
            {
                foreach (var flowPartWidget in context.ContentItem.Content.FlowPart.Widgets)
                {
                    foreach (dynamic item in flowPartWidget.FlowPart.Widgets)
                    {
                        if (item.ValidationRulePart == null) continue;
                        aspect.Add(new ValidationRuleAspect
                        {
                            FormInputName = item.FormInputElementPart.Name,
                            Type = item.ValidationRulePart.Type,
                            Option = item.ValidationRulePart.Option
                        }); ;
                    }
                }
                return Task.CompletedTask;
            });
        }
    }
}
