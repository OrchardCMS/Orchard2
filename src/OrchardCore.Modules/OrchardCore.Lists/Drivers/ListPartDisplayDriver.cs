using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Display.ViewModels;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Lists.Models;
using OrchardCore.Lists.Services;
using OrchardCore.Lists.ViewModels;
using OrchardCore.Navigation;

namespace OrchardCore.Lists.Drivers
{
    public class ListPartDisplayDriver : ContentPartDisplayDriver<ListPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContainerService _containerService;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly IStringLocalizer S;

        public ListPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IContainerService containerService,
            IUpdateModelAccessor updateModelAccessor,
            IStringLocalizer<ListPartDisplayDriver> stringLocalizer
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            _containerService = containerService;
            _updateModelAccessor = updateModelAccessor;
            S = stringLocalizer;
        }

        public override IDisplayResult Display(ListPart listPart, BuildPartDisplayContext context)
        {
            return
                Combine(
                    Initialize<ListPartViewModel>(GetDisplayShapeType(context), async model =>
                    {
                        var pager = await GetPagerSlimAsync(context);
                        var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
                        var containedItemOptions = new ContainedItemOptions();
                        model.ContentItems = (await _containerService.QueryContainedItemsAsync(
                            listPart.ContentItem.ContentItemId,
                            settings.EnableOrdering,
                            pager,
                            containedItemOptions)).ToArray();

                        model.ContainedContentTypeDefinitions = GetContainedContentTypes(context);
                        model.Context = context;
                        model.Pager = await context.New.PagerSlim(pager);
                    })
                    .Location("Detail", "Content:10"),
                    Initialize<ListPartViewModel>("ListPart", async model =>
                    {
                        var pager = await GetPagerSlimAsync(context);
                        var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
                        var listPartFilterViewModel = new ListPartFilterViewModel();
                        var containedItemOptions = new ContainedItemOptions();

                        await _updateModelAccessor.ModelUpdater.TryUpdateModelAsync(listPartFilterViewModel, Prefix);
                        model.ListPart = listPart;
                        containedItemOptions.DisplayText = listPartFilterViewModel.DisplayText;
                        containedItemOptions.Status = listPartFilterViewModel.Status;
                        model.ListPartFilterViewModel = listPartFilterViewModel;

                        model.ContentItems = (await _containerService.QueryContainedItemsAsync(
                            listPart.ContentItem.ContentItemId,
                            settings.EnableOrdering,
                            pager,
                            containedItemOptions)).ToArray();

                        model.ContainedContentTypeDefinitions = GetContainedContentTypes(context);
                        model.Context = context;
                        model.EnableOrdering = settings.EnableOrdering;
                        model.Pager = await context.New.PagerSlim(pager);

                        model.ContentsBulkAction = new List<SelectListItem>()
                        {
                            new SelectListItem() { Text = S["Publish Now"], Value = nameof(ContentsListBulkAction.PublishNow) },
                            new SelectListItem() { Text = S["Unpublish"], Value = nameof(ContentsListBulkAction.Unpublish) },
                            new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsListBulkAction.Remove) }
                        };
                    })
                    .Location("DetailAdmin", "Content:10"),
                    Initialize<ContentItemViewModel>("ListPart", model => model.ContentItem = listPart.ContentItem)
                    .Location("SummaryAdmin", "Actions:4")
                );
        }

        private async Task<PagerSlim> GetPagerSlimAsync(BuildPartDisplayContext context)
        {
            var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
            var pagerParameters = new PagerSlimParameters();
            await context.Updater.TryUpdateModelAsync(pagerParameters);

            var pager = new PagerSlim(pagerParameters, settings.PageSize);

            return pager;
        }

        private IEnumerable<ContentTypeDefinition> GetContainedContentTypes(BuildPartDisplayContext context)
        {
            var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
            var contentTypes = settings.ContainedContentTypes ?? Enumerable.Empty<string>();
            return contentTypes.Select(contentType => _contentDefinitionManager.GetTypeDefinition(contentType));
        }
    }
}
