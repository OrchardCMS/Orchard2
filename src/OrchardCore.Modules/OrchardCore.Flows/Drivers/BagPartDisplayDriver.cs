using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Flows.Models;
using OrchardCore.Flows.ViewModels;

namespace OrchardCore.Flows.Drivers
{
    public class BagPartDisplayDriver : ContentPartDisplayDriver<BagPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentManager _contentManager;
        private readonly IServiceProvider _serviceProvider;

        public BagPartDisplayDriver(
            IContentManager contentManager,
            IContentDefinitionManager contentDefinitionManager,
            IServiceProvider serviceProvider
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentManager = contentManager;
            _serviceProvider = serviceProvider;
        }

        public override IDisplayResult Display(BagPart bagPart, BuildPartDisplayContext context)
        {
            var hasItems = bagPart.ContentItems.Any();

            return Initialize<BagPartViewModel>(hasItems ? "BagPart" : "BagPart_Empty", m =>
            {
                m.BagPart = bagPart;
                m.BuildPartDisplayContext = context;
                m.Settings = context.TypePartDefinition.GetSettings<BagPartSettings>();
            })
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
        }

        public override IDisplayResult Edit(BagPart bagPart, BuildPartEditorContext context)
        {
            return Initialize<BagPartEditViewModel>("BagPart_Edit", m =>
            {
                m.BagPart = bagPart;
                m.Updater = context.Updater;
                m.ContainedContentTypeDefinitions = GetContainedContentTypes(context.TypePartDefinition);
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(BagPart part, UpdatePartEditorContext context)
        {
            var contentItemDisplayManager = _serviceProvider.GetRequiredService<IContentItemDisplayManager>();
            var model = new BagPartEditViewModel { BagPart = part };

            await context.Updater.TryUpdateModelAsync(model, Prefix);

            var contentItems = new List<ContentItem>();

            for (var i = 0; i < model.Prefixes.Length; i++)
            {
                var contentItem = await _contentManager.NewAsync(model.ContentTypes[i]);
                var existingContentItem = part.ContentItems.FirstOrDefault(x => String.Equals(x.ContentItemId, model.Prefixes[i], StringComparison.OrdinalIgnoreCase));
                // When the content item already exists merge its elements to preserve nested content item ids.
                // All of the data for these merged items is then replaced by the model values on update, while a nested content item id is maintained.
                // This prevents nested items which rely on the content item id, i.e. the media attached field, losing their reference point.
                if (existingContentItem != null)
                {
                    contentItem.ContentItemId = model.Prefixes[i];
                    contentItem.Merge(existingContentItem);
                }

                var widgetModel = await contentItemDisplayManager.UpdateEditorAsync(contentItem, context.Updater, context.IsNew, htmlFieldPrefix: model.Prefixes[i]);

                contentItems.Add(contentItem);
            }

            part.ContentItems = contentItems;

            return Edit(part, context);
        }

        private IEnumerable<ContentTypeDefinition> GetContainedContentTypes(ContentTypePartDefinition typePartDefinition)
        {
            var settings = typePartDefinition.GetSettings<BagPartSettings>();

            return settings.ContainedContentTypes
                .Select(contentType => _contentDefinitionManager.GetTypeDefinition(contentType))
                .Where(contentType => contentType != null);
        }
    }
}
