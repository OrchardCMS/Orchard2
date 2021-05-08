using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.Apis.GraphQL;
using OrchardCore.Apis.GraphQL.Resolvers;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.GraphQL
{
    public class MediaAssetQuery : ISchemaBuilder
    {
        private readonly IStringLocalizer S;

        public MediaAssetQuery(IStringLocalizer<MediaAssetQuery> localizer)
        {
            S = localizer;
        }

        public Task<string> GetIdentifierAsync() => Task.FromResult(String.Empty);

        public Task BuildAsync(ISchema schema)
        {
            var field = new FieldType
            {
                Name = "MediaAssets",
                Description = S["Media assets are items that are part of your media library."],
                Type = typeof(ListGraphType<MediaAssetObjectType>),
                Arguments = new QueryArguments(
                    new QueryArgument<StringGraphType>
                    {
                        Name = "path",
                        Description = S["Media asset path."]
                    },
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeSubDirectories",
                        Description = S["Whether to get the assets from just the top directory or from all sub-directories as well."]
                    }
                ),
                Resolver = new LockedAsyncFieldResolver<IEnumerable<IFileStoreEntry>>(ResolveAsync)
            };

            schema.Query.AddField(field);

            return Task.CompletedTask;
        }

        private async Task<IEnumerable<IFileStoreEntry>> ResolveAsync(IResolveFieldContext resolveContext)
        {
            var mediaFileStore = resolveContext.RequestServices.GetService<IMediaFileStore>();

            var path = resolveContext.GetArgument("path", string.Empty);
            var includeSubDirectories = resolveContext.GetArgument("includeSubDirectories", false);

            var allFiles = mediaFileStore.GetDirectoryContentAsync(path, includeSubDirectories);

            if (includeSubDirectories)
            {
                return await allFiles.ToListAsync();
            }
            else
            {
                return await allFiles.Where(x => !x.IsDirectory).ToListAsync();
            }
        }
    }
}
