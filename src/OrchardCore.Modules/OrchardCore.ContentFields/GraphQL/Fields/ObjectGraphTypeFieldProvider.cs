using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.GraphQL.Queries.Types;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentFields.GraphQL.Fields
{
    public class ObjectGraphTypeFieldProvider : IContentFieldProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ObjectGraphTypeFieldProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public FieldType GetField(ContentPartFieldDefinition field)
        {
            var serviceProvider = _httpContextAccessor.HttpContext.RequestServices;
            var typeActivator = serviceProvider.GetService<ITypeActivatorFactory<ContentField>>();
            var activator = typeActivator.GetTypeActivator(field.FieldDefinition.Name);

            var queryGraphType = typeof(ObjectGraphType<>).MakeGenericType(activator.Type);

            if (serviceProvider.GetService(queryGraphType) is IObjectGraphType)
            {
                return new FieldType
                {
                    Name = field.Name,
                    Description = field.FieldDefinition.Name,
                    Type = queryGraphType,
                    Resolver = new FuncFieldResolver<ContentElement, ContentElement>(context =>
                    {
                        var typeToResolve = context.FieldDefinition.ResolvedType.GetType().BaseType.GetGenericArguments().First(); // ReturnType.GetType().BaseType.GetGenericArguments().First();

                        // Check if part has been collapsed by trying to get the parent part.
                        var contentPart = context.Source.Get(typeof(ContentPart), field.PartDefinition.Name);
                        if (contentPart == null)
                        {
                            // Part is not collapsed, access field directly.
                            contentPart = context.Source;
                        }

                        var contentField = contentPart?.Get(typeToResolve, field.Name);
                        return contentField;
                    })
                };
            }

            return null;
        }
    }
}
