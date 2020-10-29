using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Contents.ViewModels;
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
        private readonly IHttpContextAccessor _hca;
        private readonly IUpdateModelAccessor _updateModelAccessor;

        public ListPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IContainerService containerService,
            IHttpContextAccessor hca,
            IUpdateModelAccessor updateModelAccessor
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            _containerService = containerService;
            _hca = hca;
            _updateModelAccessor = updateModelAccessor;
        }

        public override IDisplayResult Display(ListPart listPart, BuildPartDisplayContext context)
        {
            return
                Combine(
                    Initialize<ListPartViewModel>(GetDisplayShapeType(context), async model =>
                    {
                        var pager = await GetPagerSlimAsync(context);
                        var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
                        var containeditemOptions = new ContainedItemOptions();
                        model.ListPart = listPart;
                        model.ContentItems = (await _containerService.QueryContainedItemsAsync(listPart.ContentItem.ContentItemId, settings.EnableOrdering, pager, true, containeditemOptions)).ToArray();
                        model.ContainedContentTypeDefinitions = GetContainedContentTypes(context);
                        model.Context = context;
                        model.Pager = await context.New.PagerSlim(pager);
                    })
                    .Location("Detail", "Content:10"),
                    Initialize<ListPartViewModel>("ListPart", async model =>
                    {
                        var pager = await GetPagerSlimAsync(context);
                        var settings = context.TypePartDefinition.GetSettings<ListPartSettings>();
                        var listpartFilterViewModel = new ListPartFilterViewModel();
                        var containeditemOptions = new ContainedItemOptions();

                        await _updateModelAccessor.ModelUpdater.TryUpdateModelAsync(listpartFilterViewModel, Prefix);
                        model.ListPart = listPart;
                        containeditemOptions.DisplayText = listpartFilterViewModel.DisplayText;
                        containeditemOptions.Status = (ContentsStatus)listpartFilterViewModel.Status;
                        model.ListPartFilterViewModel = listpartFilterViewModel;

                        model.ContentItems = (await _containerService.QueryContainedItemsAsync(listPart.ContentItem.ContentItemId, settings.EnableOrdering, pager, false, containeditemOptions)).ToArray();
                        model.ContainedContentTypeDefinitions = GetContainedContentTypes(context);
                        model.Context = context;
                        model.EnableOrdering = settings.EnableOrdering;
                        model.Pager = await context.New.PagerSlim(pager);
                    })
                    .Location("DetailAdmin", "Content:10")
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
