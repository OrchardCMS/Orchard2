using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.DisplayManagement.Zones;
using OrchardCore.Modules;

namespace OrchardCore.ContentManagement.Display
{
    /// <summary>
    /// Provides a concrete implementation of a display handler coordinating part, field and content item drivers.
    /// </summary>
    public class ContentItemDisplayCoordinator : IContentDisplayHandler
    {
        private readonly IContentPartDisplayDriverResolver _contentPartDisplayDriverResolver;
        private readonly IContentFieldDisplayDriverResolver _contentFieldDisplayDriverResolver;
        private readonly IEnumerable<IContentDisplayDriver> _displayDrivers;
        private readonly IEnumerable<IContentFieldDisplayDriver> _fieldDisplayDrivers;
        private readonly IEnumerable<IContentPartDisplayDriver> _partDisplayDrivers;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ITypeActivatorFactory<ContentPart> _contentPartFactory;
        private readonly ILogger _logger;

        public ContentItemDisplayCoordinator(
            IContentPartDisplayDriverResolver contentPartDisplayDriverResolver,
            IContentFieldDisplayDriverResolver contentFieldDisplayDriverResolver,
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentDisplayDriver> displayDrivers,
            IEnumerable<IContentFieldDisplayDriver> fieldDisplayDrivers,
            IEnumerable<IContentPartDisplayDriver> partDisplayDrivers,
            ITypeActivatorFactory<ContentPart> contentPartFactory,
            ILogger<ContentItemDisplayCoordinator> logger)
        {
            _contentPartDisplayDriverResolver = contentPartDisplayDriverResolver;
            _contentFieldDisplayDriverResolver = contentFieldDisplayDriverResolver;
            _contentPartFactory = contentPartFactory;
            _contentDefinitionManager = contentDefinitionManager;
            _displayDrivers = displayDrivers;
            _fieldDisplayDrivers = fieldDisplayDrivers;
            _partDisplayDrivers = partDisplayDrivers;

            foreach (var element in partDisplayDrivers.Select(x => x.GetType()))
            {
                logger.LogWarning("The content part display driver '{ContentPartDisplayDriver}' should not be registered in DI. Use UseDisplayDriver<T> instead.", element);
            }

            foreach (var element in fieldDisplayDrivers.Select(x => x.GetType()))
            {
                logger.LogWarning("The content field display driver '{ContentFieldDisplayDriver}' should not be registered in DI. Use UseDisplayDriver<T> instead.", element);
            }

            _logger = logger;
        }

        public async Task BuildDisplayAsync(ContentItem contentItem, BuildDisplayContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

            if (contentTypeDefinition == null)
            {
                return;
            }

            foreach (var displayDriver in _displayDrivers)
            {
                try
                {
                    var result = await displayDriver.BuildDisplayAsync(contentItem, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    InvokeExtensions.HandleException(ex, _logger, displayDriver.GetType().Name, nameof(BuildDisplayAsync));
                }
            }

            var settings = contentTypeDefinition?.GetSettings<ContentTypeSettings>();
            var stereotype = "";

            if (settings != null)
            {
                stereotype = settings.Stereotype;
            }

            foreach (var contentTypePartDefinition in contentTypeDefinition.Parts)
            {
                var partName = contentTypePartDefinition.Name;
                var partTypeName = contentTypePartDefinition.PartDefinition.Name;
                var contentType = contentTypePartDefinition.ContentTypeDefinition.Name;
                var partActivator = _contentPartFactory.GetTypeActivator(partTypeName);
                var part = contentItem.Get(partActivator.Type, partName) as ContentPart;

                if (part != null)
                {
                    var partDisplayDrivers = _contentPartDisplayDriverResolver.GetDisplayModeDrivers(partTypeName, contentTypePartDefinition.DisplayMode());
                    foreach (var partDisplayDriver in partDisplayDrivers)
                    {
                        try
                        {
                            var result = await partDisplayDriver.BuildDisplayAsync(part, contentTypePartDefinition, context);
                            if (result != null)
                            {
                                await result.ApplyAsync(context);
                            }
                        }
                        catch (Exception ex)
                        {
                            InvokeExtensions.HandleException(ex, _logger, partDisplayDrivers.GetType().Name, nameof(BuildDisplayAsync));
                        }
                    }
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                    foreach (var displayDriver in _partDisplayDrivers)
                    {
                        try
                        {
                            var result = await displayDriver.BuildDisplayAsync(part, contentTypePartDefinition, context);
                            if (result != null)
                            {
                                await result.ApplyAsync(context);
                            }
                        }
                        catch (Exception ex)
                        {
                            InvokeExtensions.HandleException(ex, _logger, displayDriver.GetType().Name, nameof(BuildDisplayAsync));
                        }
                    }
                    var tempContext = context;

                    // Create a custom ContentPart shape that will hold the fields for dynamic content part (not implicit parts)
                    // This allows its fields to be grouped and templated

                    if (part.GetType() == typeof(ContentPart) && partTypeName != contentTypePartDefinition.ContentTypeDefinition.Name)
                    {
                        var displayMode = contentTypePartDefinition.DisplayMode();
                        var hasDisplayMode = !String.IsNullOrEmpty(displayMode);
                        var shapeType = partTypeName;

                        var shapeResult = new ShapeResult(shapeType, ctx => ctx.ShapeFactory.CreateAsync(shapeType, () => new ValueTask<IShape>(new ZoneHolding(() => ctx.ShapeFactory.CreateAsync("Zone")))));
                        shapeResult.Differentiator(partName);
                        shapeResult.Name(partName);
                        shapeResult.Location("Content");

                        shapeResult.Displaying(ctx =>
                        {
                            string displayToken = "_Display";
                            string[] displayTypes = new[] { "", "_" + ctx.Shape.Metadata.DisplayType };

                            foreach (var dType in displayTypes)
                            {
                                var shapeTypeSuffix = string.Empty;
                                var displayType = dType;
                                // Fall back to default template of ContentPart, if there is not template for shape type(partTypeName)
                                // eg. ContentPart, ContentPart.Summary
                                ctx.Shape.Metadata.Alternates.Add($"ContentPart{displayType}");

                                if(hasDisplayMode)
                                {
                                    shapeTypeSuffix = $"__{displayMode}";
                                    if (displayType == "")
                                    {
                                        displayType = displayToken;
                                    }
                                    else
                                    {
                                        shapeTypeSuffix = $"{shapeTypeSuffix}{displayToken}";
                                    }
                                }

                                // eg. ServicePart,  ServicePart.Summary
                                ctx.Shape.Metadata.Alternates.Add($"{partTypeName}{displayType}{shapeTypeSuffix}");

                                // [ContentType]_[DisplayType]__[PartType]
                                // e.g. LandingPage-ServicePart, LandingPage-ServicePart.Summary
                                ctx.Shape.Metadata.Alternates.Add($"{contentType}{displayType}__{partTypeName}{shapeTypeSuffix}");

                                if (!String.IsNullOrEmpty(stereotype))
                                {
                                    // [Stereotype]_[DisplayType]__[PartType],
                                    // e.g. Widget-ServicePart
                                    ctx.Shape.Metadata.Alternates.Add($"{stereotype}{displayType}__{partTypeName}{shapeTypeSuffix}");
                                }
                            }

                            if (partTypeName != partName)
                            {
                                foreach (var dType in displayTypes)
                                {
                                    var displayType = dType;
                                    var shapeTypeSuffix = string.Empty;

                                    if(hasDisplayMode)
                                    {
                                        shapeTypeSuffix = $"__{displayMode}";
                                        if (displayType == "")
                                        {
                                            displayType = displayToken;
                                        }
                                        else
                                        {
                                            shapeTypeSuffix = $"{shapeTypeSuffix}{displayToken}";
                                        }
                                    }

                                    // [ContentType]_[DisplayType]__[PartName]
                                    // e.g. Employee-Address1, Employee-Address2
                                    ctx.Shape.Metadata.Alternates.Add($"{contentType}{displayType}__{partName}{shapeTypeSuffix}");

                                    if (!String.IsNullOrEmpty(stereotype))
                                    {
                                        // [Stereotype]_[DisplayType]__[PartType]__[PartName]
                                        // e.g. Widget-Services
                                        ctx.Shape.Metadata.Alternates.Add($"{stereotype}{displayType}__{partTypeName}__{partName}{shapeTypeSuffix}");
                                    }
                                }
                            }
                        });

                        await shapeResult.ApplyAsync(context);

                        var contentPartShape = shapeResult.Shape;

                        // Make the ContentPart name property available on the shape
                        contentPartShape.Properties[partTypeName] = part.Content;
                        contentPartShape.Properties["ContentItem"] = part.ContentItem;

                        context = new BuildDisplayContext(shapeResult.Shape, context.DisplayType, context.GroupId, context.ShapeFactory, context.Layout, context.Updater);
                        // With a new display context we have the default FindPlacementDelegate that returns null, so we reuse the delegate from the temp context.
                        context.FindPlacement = tempContext.FindPlacement;
                    }

                    foreach (var contentPartFieldDefinition in contentTypePartDefinition.PartDefinition.Fields)
                    {
                        var fieldDisplayDrivers = _contentFieldDisplayDriverResolver.GetDisplayModeDrivers(contentPartFieldDefinition.FieldDefinition.Name, contentPartFieldDefinition.DisplayMode());
                        foreach (var fieldDisplayDriver in fieldDisplayDrivers)
                        {
                            try
                            {
                                var result = await fieldDisplayDriver.BuildDisplayAsync(part, contentPartFieldDefinition, contentTypePartDefinition, context);
                                if (result != null)
                                {
                                    await result.ApplyAsync(context);
                                }
                            }
                            catch (Exception ex)
                            {
                                InvokeExtensions.HandleException(ex, _logger, fieldDisplayDriver.GetType().Name, nameof(BuildDisplayAsync));
                            }
                        }
                        // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                        // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                        foreach (var displayDriver in _fieldDisplayDrivers)
                        {
                            try
                            {
                                var result = await displayDriver.BuildDisplayAsync(part, contentPartFieldDefinition, contentTypePartDefinition, context);
                                if (result != null)
                                {
                                    await result.ApplyAsync(context);
                                }
                            }
                            catch (Exception ex)
                            {
                                InvokeExtensions.HandleException(ex, _logger, displayDriver.GetType().Name, nameof(BuildDisplayAsync));
                            }
                        }
                    }

                    context = tempContext;
                }
            }
        }

        public async Task BuildEditorAsync(ContentItem contentItem, BuildEditorContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            var contentShape = context.Shape as IZoneHolding;
            var partsShape = await context.ShapeFactory.CreateAsync("ContentZone",
                Arguments.From(new
                {
                    Identifier = contentItem.ContentItemId
                }));

            contentShape.Zones["Parts"] = partsShape;

            foreach (var displayDriver in _displayDrivers)
            {
                try
                {
                    var result = await displayDriver.BuildEditorAsync(contentItem, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    InvokeExtensions.HandleException(ex, _logger, displayDriver.GetType().Name, nameof(BuildEditorAsync));
                }
            }

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partTypeName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partTypeName);
                var part = (ContentPart)contentItem.Get(activator.Type, typePartDefinition.Name) ?? activator.CreateInstance();
                part.ContentItem = contentItem;

                // Create a custom shape to render all the part shapes into it
                var typePartShape = await context.ShapeFactory.CreateAsync("ContentPart_Edit");
                typePartShape.Properties["ContentPart"] = part;
                typePartShape.Properties["ContentTypePartDefinition"] = typePartDefinition;

                var partPosition = typePartDefinition.GetSettings<ContentTypePartSettings>().Position ?? "before";

                await partsShape.AddAsync(typePartShape, partPosition);
                partsShape.Properties[typePartDefinition.Name] = typePartShape;

                context.DefaultZone = $"Parts.{typePartDefinition.Name}";
                context.DefaultPosition = partPosition;

                var partDisplayDrivers = _contentPartDisplayDriverResolver.GetEditorDrivers(partTypeName, typePartDefinition.Editor());
                await partDisplayDrivers.InvokeAsync(async (driver, part, typePartDefinition, context) =>
                {
                    var result = await driver.BuildEditorAsync(part, typePartDefinition, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }, part, typePartDefinition, context, _logger);
                // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                await _partDisplayDrivers.InvokeAsync(async (driver, part, typePartDefinition, context) =>
                {
                    var result = await driver.BuildEditorAsync(part, typePartDefinition, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }, part, typePartDefinition, context, _logger);

                foreach (var partFieldDefinition in typePartDefinition.PartDefinition.Fields)
                {
                    var fieldName = partFieldDefinition.Name;
                    var fieldPosition = partFieldDefinition.GetSettings<ContentPartFieldSettings>().Position ?? "before";

                    context.DefaultZone = $"Parts.{typePartDefinition.Name}:{fieldPosition}";
                    var fieldDisplayDrivers = _contentFieldDisplayDriverResolver.GetEditorDrivers(partFieldDefinition.FieldDefinition.Name, partFieldDefinition.Editor());
                    await fieldDisplayDrivers.InvokeAsync(async (driver, part, partFieldDefinition, typePartDefinition, context) =>
                    {
                        var result = await driver.BuildEditorAsync(part, partFieldDefinition, typePartDefinition, context);
                        if (result != null)
                        {
                            await result.ApplyAsync(context);
                        }
                    }, part, partFieldDefinition, typePartDefinition, context, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                    await _fieldDisplayDrivers.InvokeAsync(async (driver, part, partFieldDefinition, typePartDefinition, context) =>
                    {
                        var result = await driver.BuildEditorAsync(part, partFieldDefinition, typePartDefinition, context);
                        if (result != null)
                        {
                            await result.ApplyAsync(context);
                        }
                    }, part, partFieldDefinition, typePartDefinition, context, _logger);
                }
            }
        }

        public async Task UpdateEditorAsync(ContentItem contentItem, UpdateEditorContext context)
        {
            var contentTypeDefinition = _contentDefinitionManager.LoadTypeDefinition(contentItem.ContentType);
            if (contentTypeDefinition == null)
                return;

            var contentShape = context.Shape as IZoneHolding;
            var partsShape = await context.ShapeFactory.CreateAsync("ContentZone",
                Arguments.From(new
                {
                    Identifier = contentItem.ContentItemId
                }));

            contentShape.Zones["Parts"] = partsShape;

            foreach (var displayDriver in _displayDrivers)
            {
                try
                {
                    var result = await displayDriver.UpdateEditorAsync(contentItem, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    InvokeExtensions.HandleException(ex, _logger, displayDriver.GetType().Name, nameof(UpdateEditorAsync));
                }
            }

            foreach (var typePartDefinition in contentTypeDefinition.Parts)
            {
                var partTypeName = typePartDefinition.PartDefinition.Name;
                var activator = _contentPartFactory.GetTypeActivator(partTypeName);
                var part = (ContentPart)contentItem.Get(activator.Type, typePartDefinition.Name) ?? activator.CreateInstance();
                part.ContentItem = contentItem;

                // Create a custom shape to render all the part shapes into it
                var typePartShape = await context.ShapeFactory.CreateAsync("ContentPart_Edit");
                typePartShape.Properties["ContentPart"] = part;
                typePartShape.Properties["ContentTypePartDefinition"] = typePartDefinition;
                var partPosition = typePartDefinition.GetSettings<ContentTypePartSettings>().Position ?? "before";

                await partsShape.AddAsync(typePartShape, partPosition);
                partsShape.Properties[typePartDefinition.Name] = typePartShape;

                context.DefaultZone = $"Parts.{typePartDefinition.Name}:{partPosition}";
                var partDisplayDrivers = _contentPartDisplayDriverResolver.GetEditorDrivers(partTypeName, typePartDefinition.Editor());
                await partDisplayDrivers.InvokeAsync(async (driver, part, typePartDefinition, context) =>
                {
                    var result = await driver.UpdateEditorAsync(part, typePartDefinition, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }, part, typePartDefinition, context, _logger);
                // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                await _partDisplayDrivers.InvokeAsync(async (driver, part, typePartDefinition, context) =>
                {
                    var result = await driver.UpdateEditorAsync(part, typePartDefinition, context);
                    if (result != null)
                    {
                        await result.ApplyAsync(context);
                    }
                }, part, typePartDefinition, context, _logger);

                foreach (var partFieldDefinition in typePartDefinition.PartDefinition.Fields)
                {
                    var fieldName = partFieldDefinition.Name;
                    var fieldPosition = partFieldDefinition.GetSettings<ContentPartFieldSettings>().Position ?? "before";

                    context.DefaultZone = $"Parts.{typePartDefinition.Name}:{fieldPosition}";
                    var fieldDisplayDrivers = _contentFieldDisplayDriverResolver.GetEditorDrivers(partFieldDefinition.FieldDefinition.Name, partFieldDefinition.Editor());
                    await fieldDisplayDrivers.InvokeAsync(async (driver, part, partFieldDefinition, typePartDefinition, context) =>
                    {
                        var result = await driver.UpdateEditorAsync(part, partFieldDefinition, typePartDefinition, context);
                        if (result != null)
                        {
                            await result.ApplyAsync(context);
                        }
                    }, part, partFieldDefinition, typePartDefinition, context, _logger);
                    // TODO: This can be removed in a future release as the recommended way is to use ContentOptions.
                    // Iterate existing driver registrations as multiple drivers maybe not be registered with ContentOptions.
                    await _fieldDisplayDrivers.InvokeAsync(async (driver, part, partFieldDefinition, typePartDefinition, context) =>
                    {
                        var result = await driver.UpdateEditorAsync(part, partFieldDefinition, typePartDefinition, context);
                        if (result != null)
                        {
                            await result.ApplyAsync(context);
                        }
                    }, part, partFieldDefinition, typePartDefinition, context, _logger);
                }
            }
        }
    }
}
