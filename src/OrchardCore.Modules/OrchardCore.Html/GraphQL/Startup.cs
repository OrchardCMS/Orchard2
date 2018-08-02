using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Apis;
using OrchardCore.Html.Model;
using OrchardCore.Modules;

namespace OrchardCore.Html.GraphQL
{
    [RequireFeatures("OrchardCore.Apis.GraphQL")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQLInputType<HtmlBodyPart, HtmlBodyInputObjectType>();
            services.AddGraphQLQueryType<HtmlBodyPart, HtmlBodyQueryObjectType>();
        }
    }
}
