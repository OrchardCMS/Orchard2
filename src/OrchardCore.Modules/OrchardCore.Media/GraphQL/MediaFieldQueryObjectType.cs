using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Apis.GraphQL;
using OrchardCore.Media.Fields;

namespace OrchardCore.Media.GraphQL
{
    public class MediaFieldQueryObjectType : ObjectGraphType<MediaField>
    {
        public MediaFieldQueryObjectType()
        {
            Name = nameof(MediaField);

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>()
                .Name("paths")
                .Description("the media paths")
                .PagingArguments()
                .Resolve(x => x.Page(x.Source.Paths));

            Field<ListGraphType<StringGraphType>, IEnumerable<string>>()
                .Name("urls")
                .Description("the absolute urls of the media items")
                .PagingArguments()
                .Resolve(x =>
                {
                    var paths = x.Page(x.Source.Paths);
                    var context = (GraphQLUserContext)x.UserContext;
                    var mediaFileStore = x.RequestServices.GetService<IMediaFileStore>();
                    return paths.Select(p => mediaFileStore.MapPathToPublicUrl(p));
                });
        }
    }
}
