using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.Admin;
using OrchardCore.ContentManagement.Display;
using OrchardCore.DisplayManagement.Layout;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.DisplayManagement.Zones;
using OrchardCore.Environment.Cache;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Layers.Handlers;
using OrchardCore.Layers.ViewModels;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Scripting;

namespace OrchardCore.Layers.Services
{
    public class LayerFilter : IAsyncResultFilter
    {
        private readonly ILayoutAccessor _layoutAccessor;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IUpdateModelAccessor _modelUpdaterAccessor;
        private readonly IScriptingManager _scriptingManager;
        private readonly IMemoryCache _memoryCache;
        private readonly ISignal _signal;
        private readonly IThemeManager _themeManager;
        private readonly IAdminThemeService _adminThemeService;
        private readonly ILayerService _layerService;

        public LayerFilter(
            ILayerService layerService,
            ILayoutAccessor layoutAccessor,
            IContentItemDisplayManager contentItemDisplayManager,
            IUpdateModelAccessor modelUpdaterAccessor,
            IScriptingManager scriptingManager,
            IMemoryCache memoryCache,
            ISignal signal,
            IThemeManager themeManager,
            IAdminThemeService adminThemeService)
        {
            _layerService = layerService;
            _layoutAccessor = layoutAccessor;
            _contentItemDisplayManager = contentItemDisplayManager;
            _modelUpdaterAccessor = modelUpdaterAccessor;
            _scriptingManager = scriptingManager;
            _memoryCache = memoryCache;
            _signal = signal;
            _themeManager = themeManager;
            _adminThemeService = adminThemeService;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Should only run on the front-end for a full view
            if ((context.Result is ViewResult || context.Result is PageResult) &&
                !AdminAttribute.IsApplied(context.HttpContext))
            {
                // Even if the Admin attribute is not applied we might be using the admin theme, for instance in Login views.
                // In this case don't render Layers.
                var selectedTheme = (await _themeManager.GetThemeAsync())?.Id;
                var adminTheme = await _adminThemeService.GetAdminThemeNameAsync();
                if (selectedTheme == adminTheme)
                {
                    await next.Invoke();
                    return;
                }

                var cacheKey = $"OrchardCore.Layers.LayerFilter:AllWidgets-{CultureInfo.CurrentUICulture.Name}";
                var widgets = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AddExpirationToken(_signal.GetToken(LayerMetadataHandler.LayerChangeToken));

                    var allWidgets = await _layerService.GetLayerWidgetsMetadataAsync(x => x.Published);
                    return await _layerService
                        .FilterWidgetsByCultureAsync(allWidgets, CultureInfo.CurrentUICulture.Name)
                        .ToListAsync();
                });

                var layers = (await _layerService.GetLayersAsync()).Layers.ToDictionary(x => x.Name);

                dynamic layout = await _layoutAccessor.GetLayoutAsync();
                var updater = _modelUpdaterAccessor.ModelUpdater;

                var engine = _scriptingManager.GetScriptingEngine("js");
                var scope = engine.CreateScope(_scriptingManager.GlobalMethodProviders.SelectMany(x => x.GetMethods()), ShellScope.Services, null, null);

                var layersCache = new Dictionary<string, bool>();

                foreach (var widget in widgets)
                {
                    var layer = layers[widget.Layer];

                    if (layer == null)
                    {
                        continue;
                    }

                    bool display;
                    if (!layersCache.TryGetValue(layer.Name, out display))
                    {
                        if (String.IsNullOrEmpty(layer.Rule))
                        {
                            display = false;
                        }
                        else
                        {
                            display = Convert.ToBoolean(engine.Evaluate(scope, layer.Rule));
                        }

                        layersCache[layer.Rule] = display;
                    }

                    if (!display)
                    {
                        continue;
                    }

                    var widgetContent = await _contentItemDisplayManager.BuildDisplayAsync(widget.ContentItem, updater);

                    widgetContent.Classes.Add("widget");
                    widgetContent.Classes.Add("widget-" + widget.ContentItem.ContentType.HtmlClassify());

                    var wrapper = new WidgetWrapper
                    {
                        Widget = widget.ContentItem,
                        Content = widgetContent
                    };

                    wrapper.Metadata.Alternates.Add("Widget_Wrapper__" + widget.ContentItem.ContentType);
                    wrapper.Metadata.Alternates.Add("Widget_Wrapper__Zone__" + widget.Zone);

                    var contentZone = layout.Zones[widget.Zone];

                    if (contentZone is ZoneOnDemand zoneOnDemand)
                    {
                        await zoneOnDemand.AddAsync(wrapper);
                    }
                    else
                    {
                        contentZone.Add(wrapper);
                    }
                }
            }

            await next.Invoke();
        }
    }
}
