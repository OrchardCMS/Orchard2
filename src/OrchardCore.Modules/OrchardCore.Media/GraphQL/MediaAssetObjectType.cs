using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Apis.GraphQL;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.GraphQL
{
    public class MediaAssetObjectType : ObjectGraphType<IFileStoreEntry>
    {
        public MediaAssetObjectType()
        {
            Name = "MediaAsset";

            Field(file => file.Name).Description("The name of the asset.");

            Field<StringGraphType>()
                .Name("path")
                .Description("The url to the asset.")
                .Resolve(x =>
                {
                    var path = x.Source.Path;
                    var context = (GraphQLUserContext)x.UserContext;
                    var mediaFileStore = x.RequestServices.GetService<IMediaFileStore>();
                    return mediaFileStore.MapPathToPublicUrl(path);
                });

            Field(file => file.Length).Description("The length of the file.");
            Field<DateTimeGraphType>("lastModifiedUtc", resolve: file => file.Source.LastModifiedUtc, description: "The date and time in UTC when the asset was last modified.");
        }
    }
}
