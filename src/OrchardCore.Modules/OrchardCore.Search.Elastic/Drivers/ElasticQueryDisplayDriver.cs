using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Search.Elastic.ViewModels;
using OrchardCore.Queries;

namespace OrchardCore.Search.Elastic.Drivers
{
    public class ElasticQueryDisplayDriver : DisplayDriver<Query, ElasticQuery>
    {
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;
        private readonly IStringLocalizer S;

        public ElasticQueryDisplayDriver(
            IStringLocalizer<ElasticQueryDisplayDriver> stringLocalizer,
            ElasticIndexSettingsService elasticIndexSettingsService)
        {
            _elasticIndexSettingsService = elasticIndexSettingsService;
            S = stringLocalizer;
        }

        public override IDisplayResult Display(ElasticQuery query, IUpdateModel updater)
        {
            return Combine(
                Dynamic("ElasticQuery_SummaryAdmin", model => { model.Query = query; }).Location("Content:5"),
                Dynamic("ElasticQuery_Buttons_SummaryAdmin", model => { model.Query = query; }).Location("Actions:2")
            );
        }

        public override IDisplayResult Edit(ElasticQuery query, IUpdateModel updater)
        {
            return Initialize<ElasticQueryViewModel>("ElasticQuery_Edit", async model =>
            {
                model.Query = query.Template;
                model.Index = query.Index;
                model.ReturnContentItems = query.ReturnContentItems;
                model.Indices = (await _elasticIndexSettingsService.GetSettingsAsync()).Select(x => x.IndexName).ToArray();

                // Extract query from the query string if we come from the main query editor
                if (String.IsNullOrEmpty(query.Template))
                {
                    await updater.TryUpdateModelAsync(model, "", m => m.Query);
                }
            }).Location("Content:5");
        }

        public override async Task<IDisplayResult> UpdateAsync(ElasticQuery model, IUpdateModel updater)
        {
            var viewModel = new ElasticQueryViewModel();
            if (await updater.TryUpdateModelAsync(viewModel, Prefix, m => m.Query, m => m.Index, m => m.ReturnContentItems))
            {
                model.Template = viewModel.Query;
                model.Index = viewModel.Index;
                model.ReturnContentItems = viewModel.ReturnContentItems;
            }

            if (String.IsNullOrWhiteSpace(model.Template))
            {
                updater.ModelState.AddModelError(nameof(model.Template), S["The query field is required"]);
            }

            if (String.IsNullOrWhiteSpace(model.Index))
            {
                updater.ModelState.AddModelError(nameof(model.Index), S["The index field is required"]);
            }

            return Edit(model, updater);
        }
    }
}
