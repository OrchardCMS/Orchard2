using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Apis.GraphQL;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.GraphQL.Queries;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Layers.Models;
using OrchardCore.Layers.Services;
using OrchardCore.Rules;

namespace OrchardCore.Layers.GraphQL
{
    public class LayerQueryObjectType : ObjectGraphType<Layer>
    {
        public LayerQueryObjectType()
        {
            Name = "Layer";

            Field(layer => layer.Name).Description("The name of the layer.");
            #pragma warning disable 0618
            Field(layer => layer.Rule).Description("Deprecated. The rule that activates the layer.");
            #pragma warning restore 0618
            Field<ListGraphType<StringGraphType>, IEnumerable<Condition>>()
                .Name("layerrule")
                .Description("The rule that activates the layer.")
                .Resolve(ctx => ctx.Source.LayerRule.Conditions);
            Field(layer => layer.Description).Description("The description of the layer.");
            Field<ListGraphType<LayerWidgetQueryObjectType>, IEnumerable<ContentItem>>()
                .Name("widgets")
                .Description("The widgets for this layer.")
                .Argument<PublicationStatusGraphType, PublicationStatusEnum>("status", "publication status of the widgets")
                .ResolveLockedAsync(async ctx =>
                {
                    var layerService = ctx.RequestServices.GetService<ILayerService>();

                    var filter = GetVersionFilter(ctx.GetArgument<PublicationStatusEnum>("status"));
                    var widgets = await layerService.GetLayerWidgetsAsync(filter);

                    var layerWidgets = widgets?.Where(item =>
                    {
                        var metadata = item.As<LayerMetadata>();
                        if (metadata == null) return false;
                        return metadata.Layer == ctx.Source.Name;
                    });

                    return layerWidgets;
                });
        }

        private Expression<Func<ContentItemIndex, bool>> GetVersionFilter(PublicationStatusEnum status)
        {
            switch (status)
            {
                case PublicationStatusEnum.Published: return x => x.Published;
                case PublicationStatusEnum.Draft: return x => x.Latest && !x.Published;
                case PublicationStatusEnum.Latest: return x => x.Latest;
                case PublicationStatusEnum.All: return x => true;
                default: return x => x.Published;
            }
        }
    }
}
