using System.Linq;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.GraphQL.Options;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentManagement.GraphQL.Queries.Types
{
    public class TypedContentTypeBuilder : IContentTypeBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GraphQLContentOptions _contentOptions;

        public TypedContentTypeBuilder(IHttpContextAccessor httpContextAccessor,
            IOptions<GraphQLContentOptions> contentOptionsAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _contentOptions = contentOptionsAccessor.Value;
        }

        public void Build(FieldType contentQuery, ContentTypeDefinition contentTypeDefinition, ContentItemType contentItemType)
        {
            var serviceProvider = _httpContextAccessor.HttpContext.RequestServices;
            var typeActivator = serviceProvider.GetService<ITypeActivatorFactory<ContentPart>>();

            foreach (var part in contentTypeDefinition.Parts)
            {
                if (_contentOptions.ShouldSkip(part)) continue;

                var partName = part.Name;

                // Check if another builder has already added a field for this part.
                if (contentItemType.HasField(partName)) continue;

                var activator = typeActivator.GetTypeActivator(part.PartDefinition.Name);

                var queryGraphType = typeof(ObjectGraphType<>).MakeGenericType(activator.Type);

                var collapsePart = _contentOptions.ShouldCollapse(part);

                if (serviceProvider.GetService(queryGraphType) is IObjectGraphType queryGraphTypeResolved)
                {
                    if (collapsePart)
                    {
                        foreach (var field in queryGraphTypeResolved.Fields)
                        {
                            if (_contentOptions.ShouldSkip(queryGraphType, field.Name)) continue;

                            var rolledUpField = new FieldType
                            {
                                Name = field.Name,
                                Type = field.Type,
                                Description = field.Description,
                                DeprecationReason = field.DeprecationReason,
                                Arguments = field.Arguments,
                                Resolver = new FuncFieldResolver<ContentItem, object>(context =>
                                {
                                    var nameToResolve = partName;
                                    var resolvedPart = context.Source.Get(activator.Type, nameToResolve);

                                    return field.Resolver.Resolve(new ResolveFieldContext
                                    {
                                        Arguments = context.Arguments,
                                        Source = resolvedPart,
                                        FieldDefinition = field,
                                        UserContext = context.UserContext
                                    });
                                })
                            };

                            contentItemType.AddField(rolledUpField);
                        }
                    }
                    else
                    {
                        contentItemType.Field(
                            queryGraphTypeResolved.GetType(),
                            partName.ToFieldName(),
                            description: queryGraphTypeResolved.Description,
                            resolve: context =>
                            {
                                var nameToResolve = partName;
                                var typeToResolve = context.FieldDefinition.ResolvedType.GetType().BaseType.GetGenericArguments().First();

                                return context.Source.Get(typeToResolve, nameToResolve);
                            });
                    }
                }

                var inputGraphType = typeof(InputObjectGraphType<>).MakeGenericType(activator.Type);

                if (serviceProvider.GetService(inputGraphType) is IInputObjectGraphType inputGraphTypeResolved)
                {
                    var whereArgument = contentQuery.Arguments.FirstOrDefault(x => x.Name == "where");
                    if (whereArgument == null)
                    {
                        return;
                    }

                    var whereInput = (ContentItemWhereInput)whereArgument.ResolvedType;

                    if (collapsePart)
                    {
                        foreach (var field in inputGraphTypeResolved.Fields)
                        {
                            whereInput.AddField(field.WithPartCollapsedMetaData().WithPartNameMetaData(partName));
                        }
                    }
                    else
                    {
                        whereInput.AddField(new FieldType
                        {
                            Type = inputGraphTypeResolved.GetType(),
                            Name = partName.ToFieldName(),
                            Description = inputGraphTypeResolved.Description
                        }.WithPartNameMetaData(partName));
                    }
                }
            }
        }
    }
}
