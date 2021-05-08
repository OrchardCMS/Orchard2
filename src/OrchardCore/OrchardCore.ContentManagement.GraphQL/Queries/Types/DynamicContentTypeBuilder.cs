using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.GraphQL.Options;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentManagement.GraphQL.Queries.Types
{
    public class DynamicContentTypeBuilder : IContentTypeBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GraphQLContentOptions _contentOptions;
        private readonly IStringLocalizer S;
        private readonly Dictionary<string, FieldType> _dynamicPartFields;

        public DynamicContentTypeBuilder(IHttpContextAccessor httpContextAccessor,
            IOptions<GraphQLContentOptions> contentOptionsAccessor,
            IStringLocalizer<DynamicContentTypeBuilder> localizer)
        {
            _httpContextAccessor = httpContextAccessor;
            _contentOptions = contentOptionsAccessor.Value;
            _dynamicPartFields = new Dictionary<string, FieldType>();

            S = localizer;
        }

        public void Build(FieldType contentQuery, ContentTypeDefinition contentTypeDefinition, ContentItemType contentItemType)
        {
            var serviceProvider = _httpContextAccessor.HttpContext.RequestServices;
            var contentFieldProviders = serviceProvider.GetServices<IContentFieldProvider>().ToList();

            foreach (var part in contentTypeDefinition.Parts)
            {
                var partName = part.Name;

                // This builder only handles parts with fields.
                if (!part.PartDefinition.Fields.Any()) 
                {
                    continue;
                }

                if (_contentOptions.ShouldSkip(part)) 
                {
                    continue;
                }

                if (_contentOptions.ShouldCollapse(part))
                {
                    foreach (var field in part.PartDefinition.Fields)
                    {
                        foreach (var fieldProvider in contentFieldProviders)
                        {
                            var fieldType = fieldProvider.GetField(field);

                            if (fieldType != null)
                            {
                                if (_contentOptions.ShouldSkip(fieldType.Type, fieldType.Name)) 
                                {
                                    continue;
                                }

                                contentItemType.AddField(fieldType);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Check if another builder has already added a field for this part.
                    var existingField = contentItemType.GetField(partName.ToFieldName());
                    if (existingField != null)
                    {
                        // Add content field types.
                        foreach (var field in part.PartDefinition.Fields)
                        {
                            foreach (var fieldProvider in contentFieldProviders)
                            {
                                var contentFieldType = fieldProvider.GetField(field);

                                if (contentFieldType != null && !contentItemType.HasField(contentFieldType.Name))
                                {
                                    contentItemType.AddField(contentFieldType);
                                    break;
                                }
                            }
                        }
                        continue;
                    }

                    if (_dynamicPartFields.TryGetValue(partName, out var fieldType))
                    {
                        contentItemType.AddField(fieldType);
                    }
                    else
                    {
                        var field = contentItemType.Field(
                            typeof(DynamicPartGraphType),
                            partName.ToFieldName(),
                            description: S["Represents a {0}.", part.PartDefinition.Name],
                            resolve: context =>
                            {
                                var nameToResolve = partName;
                                var typeToResolve = context.FieldDefinition.ResolvedType.GetType().BaseType.GetGenericArguments().First();

                                return context.Source.Get(typeToResolve, nameToResolve);
                            });

                        field.ResolvedType = new DynamicPartGraphType(_httpContextAccessor, part);
                        _dynamicPartFields[partName] = field;
                    }
                }
            }
        }

        public void Clear()
        {
            _dynamicPartFields.Clear();
        }
    }
}
