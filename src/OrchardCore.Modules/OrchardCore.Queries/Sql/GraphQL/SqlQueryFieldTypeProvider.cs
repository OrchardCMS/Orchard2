using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Apis.GraphQL;
using OrchardCore.Apis.GraphQL.Resolvers;
using OrchardCore.ContentManagement.GraphQL.Queries;

namespace OrchardCore.Queries.Sql.GraphQL.Queries
{
    /// <summary>
    /// This implementation of <see cref="ISchemaBuilder"/> registers
    /// all SQL Queries as GraphQL queries.
    /// </summary>
    public class SqlQueryFieldTypeProvider : ISchemaBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SqlQueryFieldTypeProvider> _logger;

        public SqlQueryFieldTypeProvider(IHttpContextAccessor httpContextAccessor, ILogger<SqlQueryFieldTypeProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public Task<string> GetIdentifierAsync()
        {
            var queryManager = _httpContextAccessor.HttpContext.RequestServices.GetService<IQueryManager>();
            return queryManager.GetIdentifierAsync();
        }

        public async Task BuildAsync(ISchema schema)
        {
            var queryManager = _httpContextAccessor.HttpContext.RequestServices.GetService<IQueryManager>();

            var queries = await queryManager.ListQueriesAsync();

            foreach (var query in queries.OfType<SqlQuery>())
            {
                if (String.IsNullOrWhiteSpace(query.Schema))
                    continue;

                var name = query.Name;

                try
                {
                    var querySchema = JObject.Parse(query.Schema);
                    if (!querySchema.ContainsKey("type"))
                    {
                        _logger.LogError("The Query '{Name}' schema is invalid, the 'type' property was not found.", name);
                        continue;
                    }
                    var type = querySchema["type"].ToString();
                    FieldType fieldType;

                    var fieldTypeName = querySchema["fieldTypeName"]?.ToString() ?? query.Name;

                    if (type.StartsWith("ContentItem/", StringComparison.OrdinalIgnoreCase))
                    {
                        var contentType = type.Remove(0, 12);
                        fieldType = BuildContentTypeFieldType(schema, contentType, query, fieldTypeName);
                    }
                    else
                    {
                        fieldType = BuildSchemaBasedFieldType(query, querySchema, fieldTypeName);
                    }

                    if (fieldType != null)
                    {
                        schema.Query.AddField(fieldType);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "The Query '{Name}' has an invalid schema.", name);
                }
            }
        }

        private FieldType BuildSchemaBasedFieldType(SqlQuery query, JToken querySchema, string fieldTypeName)
        {
            var properties = querySchema["properties"];
            if (properties == null)
            {
                return null;
            }

            var typetype = new ObjectGraphType<JObject>
            {
                Name = fieldTypeName
            };

            foreach (JProperty child in properties.Children())
            {
                var name = child.Name;
                var nameLower = name.Replace('.', '_');
                var type = child.Value["type"].ToString();
                var description = child.Value["description"]?.ToString();

                if (type == "string")
                {
                    var field = typetype.Field(
                        typeof(StringGraphType),
                        nameLower,
                        description: description,
                        resolve: context =>
                        {
                            var source = context.Source;
                            return source[context.FieldDefinition.Metadata["Name"].ToString()].ToObject<string>();
                        });
                    field.Metadata.Add("Name", name);
                }
                else if (type == "integer")
                {
                    var field = typetype.Field(
                        typeof(IntGraphType),
                        nameLower,
                        description: description,
                        resolve: context =>
                        {
                            var source = context.Source;
                            return source[context.FieldDefinition.Metadata["Name"].ToString()].ToObject<int>();
                        });
                    field.Metadata.Add("Name", name);
                }
            }

            var fieldType = new FieldType
            {
                Arguments = new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "parameters" }
                ),

                Name = fieldTypeName,
                Description = "Represents the " + query.Source + " Query : " + query.Name,
                ResolvedType = new ListGraphType(typetype),
                Resolver = new LockedAsyncFieldResolver<object, object>(async context =>
                {
                    var queryManager = context.RequestServices.GetService<IQueryManager>();
                    var iquery = await queryManager.GetQueryAsync(query.Name);

                    var parameters = context.GetArgument<string>("parameters");

                    var queryParameters = parameters != null ?
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters)
                        : new Dictionary<string, object>();

                    var result = await queryManager.ExecuteQueryAsync(iquery, queryParameters);
                    return result.Items;
                }),
                Type = typeof(ListGraphType<ObjectGraphType<JObject>>)
            };

            return fieldType;
        }

        private FieldType BuildContentTypeFieldType(ISchema schema, string contentType, SqlQuery query, string fieldTypeName)
        {
            var typetype = schema.Query.Fields.OfType<ContentItemsFieldType>().FirstOrDefault(x => x.Name == contentType);
            if (typetype == null)
            {
                return null;
            }

            var fieldType = new FieldType
            {
                Arguments = new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "parameters" }
                ),

                Name = fieldTypeName,
                Description = "Represents the " + query.Source + " Query : " + query.Name,
                ResolvedType = typetype.ResolvedType,
                Resolver = new LockedAsyncFieldResolver<object, object>(async context =>
                {
                    var queryManager = context.RequestServices.GetService<IQueryManager>();
                    var iquery = await queryManager.GetQueryAsync(query.Name);

                    var parameters = context.GetArgument<string>("parameters");

                    var queryParameters = parameters != null ?
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters)
                        : new Dictionary<string, object>();

                    var result = await queryManager.ExecuteQueryAsync(iquery, queryParameters);
                    return result.Items;
                }),
                Type = typetype.Type
            };

            return fieldType;
        }
    }
}
