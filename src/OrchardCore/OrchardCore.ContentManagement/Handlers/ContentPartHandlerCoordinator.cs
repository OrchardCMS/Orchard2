using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Modules;

namespace OrchardCore.ContentManagement.Handlers
{
    /// <summary>
    /// This component coordinates how parts are affecting content items.
    /// </summary>
    public class ContentPartHandlerCoordinator : ContentHandlerBase
    {
        private readonly IContentPartHandlerResolver _contentPartHandlerResolver;
        private readonly ITypeActivatorFactory<ContentPart> _contentPartFactory;
        private readonly IEnumerable<IContentPartHandler> _partHandlers;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ITypeActivatorFactory<ContentField> _contentFieldFactory;
        private readonly ILogger _logger;

        public ContentPartHandlerCoordinator(
            IContentPartHandlerResolver contentPartHandlerResolver,
            ITypeActivatorFactory<ContentPart> contentPartFactory,
            IEnumerable<IContentPartHandler> partHandlers,
            ITypeActivatorFactory<ContentField> contentFieldFactory,
            IContentDefinitionManager contentDefinitionManager,
            ILogger<ContentPartHandlerCoordinator> logger)
        {
            _contentPartHandlerResolver = contentPartHandlerResolver;
            _contentPartFactory = contentPartFactory;
            _contentFieldFactory = contentFieldFactory;
            _partHandlers = partHandlers;
            _contentDefinitionManager = contentDefinitionManager;

            foreach (var element in partHandlers.Select(x => x.GetType()))
            {
                logger.LogWarning("The content part handler '{ContentPartHandler}' should not be registered in DI. Use AddHandler<T> instead.", element);
            }

            _logger = logger;
        }

        public override async Task ActivatingAsync(ActivatingContentContext context)
        {
            // This method is called on New()
            // Adds all the parts to a content item based on the content type definition.

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;

                // We create the part from it's known type or from a generic one
                var part = _contentPartFactory.GetTypeActivator(partName).CreateInstance();
                var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                await partHandlers.InvokeAsync((handler, context, part) => handler.ActivatingAsync(context, part), context, part, _logger);
                // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                await _partHandlers.InvokeAsync((handler, context, part) => handler.ActivatingAsync(context, part), context, part, _logger);

                context.ContentItem.Weld(typePartDefinition.Name, part);

                foreach (var partFieldDefinition in typePartDefinition.PartDefinition.Fields)
                {
                    var fieldName = partFieldDefinition.Name;

                    if (!part.Has(fieldName))
                    {
                        var fieldActivator = _contentFieldFactory.GetTypeActivator(partFieldDefinition.FieldDefinition.Name);
                        part.Weld(fieldName, fieldActivator.CreateInstance());
                    }
                }

            }
        }

        public override async Task ActivatedAsync(ActivatedContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, partName) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ActivatedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ActivatedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task CreatingAsync(CreateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.CreatingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.CreatingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task CreatedAsync(CreateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.CreatedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.CreatedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task ImportingAsync(ImportContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ImportingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iteratate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ImportingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task ImportedAsync(ImportContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ImportedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iteratate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ImportedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task InitializingAsync(InitializingContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;
                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.InitializingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.InitializingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task InitializedAsync(InitializingContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.InitializedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.InitializedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task LoadingAsync(LoadContentContext context)
        {
            // This method is called on Get()
            // Adds all the missing parts to a content item based on the content type definition.
            // A part is missing if the content type is changed and an old content item is loaded,
            // like edited.

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
            {
                return;
            }

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                // If no existing part was not found in the content item, create a new one
                if (part == null)
                {
                    part = activator.CreateInstance();
                    context.ContentItem.Weld(typePartDefinition.Name, part);
                }

                var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                await partHandlers.InvokeAsync((handler, context, part) => handler.LoadingAsync(context, part), context, part, _logger);
                // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                await _partHandlers.InvokeAsync((handler, context, part) => handler.LoadingAsync(context, part), context, part, _logger);
                foreach (var partFieldDefinition in typePartDefinition.PartDefinition.Fields)
                {
                    var fieldName = partFieldDefinition.Name;

                    if (!part.Has(fieldName))
                    {
                        var fieldActivator = _contentFieldFactory.GetTypeActivator(partFieldDefinition.FieldDefinition.Name);
                        part.Weld(fieldName, fieldActivator.CreateInstance());
                    }
                }
            }
        }

        public override async Task LoadedAsync(LoadContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.LoadedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.LoadedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task ValidatingAsync(ValidateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ValidatingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iteratate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ValidatingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task ValidatedAsync(ValidateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ValidatedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iteratate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ValidatedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task DraftSavingAsync(SaveDraftContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.DraftSavingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.DraftSavingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task DraftSavedAsync(SaveDraftContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.DraftSavedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.DraftSavedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task PublishingAsync(PublishContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.PublishingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.PublishingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task PublishedAsync(PublishContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.PublishedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.PublishedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task RemovingAsync(RemoveContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.RemovingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.RemovingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task RemovedAsync(RemoveContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.RemovedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.RemovedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task UnpublishingAsync(PublishContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.UnpublishingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.UnpublishingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task UnpublishedAsync(PublishContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.UnpublishedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.UnpublishedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task UpdatingAsync(UpdateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.UpdatingAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.UpdatingAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task UpdatedAsync(UpdateContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart; ;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.UpdatedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.UpdatedAsync(context, part), context, part, _logger);
                }
            }
        }

        public override async Task VersioningAsync(VersionContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var buildingPart = context.BuildingContentItem.Get(activator.Type, partName) as ContentPart;
                var existingPart = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (buildingPart != null && existingPart != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, existingPart, buildingPart) => handler.VersioningAsync(context, existingPart, buildingPart), context, existingPart, buildingPart, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, existingPart, buildingPart) => handler.VersioningAsync(context, existingPart, buildingPart), context, existingPart, buildingPart, _logger);
                }
            }
        }

        public override async Task VersionedAsync(VersionContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);

                var buildingPart = (ContentPart)context.BuildingContentItem.Get(activator.Type, partName);
                var existingPart = (ContentPart)context.ContentItem.Get(activator.Type, typePartDefinition.Name);

                if (buildingPart != null && existingPart != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, existingPart, buildingPart) => handler.VersionedAsync(context, existingPart, buildingPart), context, existingPart, buildingPart, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, existingPart, buildingPart) => handler.VersionedAsync(context, existingPart, buildingPart), context, existingPart, buildingPart, _logger);
                }
            }
        }

        public override async Task GetContentItemAspectAsync(ContentItemAspectContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.GetContentItemAspectAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.GetContentItemAspectAsync(context, part), context, part, _logger);
                }
            }
        }
        public override async Task ClonedAsync(CloneContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.ClonedAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.ClonedAsync(context, part), context, part, _logger);
                }
            }
        }
        public override async Task CloningAsync(CloneContentContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partName);
                var part = context.ContentItem.Get(activator.Type, typePartDefinition.Name) as ContentPart;

                if (part != null)
                {
                    var partHandlers = _contentPartHandlerResolver.GetHandlers(partName);
                    await partHandlers.InvokeAsync((handler, context, part) => handler.CloningAsync(context, part), context, part, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing handler registrations as multiple handlers maybe not be registered with ContentOptions.=
                    await _partHandlers.InvokeAsync((handler, context, part) => handler.CloningAsync(context, part), context, part, _logger);
                }
            }
        }
    }
}
