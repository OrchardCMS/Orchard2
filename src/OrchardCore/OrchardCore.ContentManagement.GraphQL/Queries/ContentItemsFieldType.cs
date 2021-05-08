using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrchardCore.Apis.GraphQL;
using OrchardCore.Apis.GraphQL.Queries;
using OrchardCore.Apis.GraphQL.Resolvers;
using OrchardCore.ContentManagement.GraphQL.Options;
using OrchardCore.ContentManagement.GraphQL.Queries.Predicates;
using OrchardCore.ContentManagement.GraphQL.Queries.Types;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Environment.Shell;
using YesSql;
using Expression = OrchardCore.ContentManagement.GraphQL.Queries.Predicates.Expression;

namespace OrchardCore.ContentManagement.GraphQL.Queries
{
    /// <summary>
    /// This type is used by <see cref="ContentTypeQuery"/> to represent a query on a content type
    /// </summary>
    public class ContentItemsFieldType : FieldType
    {
        private static readonly List<string> ContentItemProperties;
        private readonly int _defaultNumberOfItems;

        static ContentItemsFieldType()
        {
            ContentItemProperties = new List<string>();

            foreach (var property in typeof(ContentItemIndex).GetProperties())
            {
                ContentItemProperties.Add(property.Name);
            }
        }

        public ContentItemsFieldType(string contentItemName, ISchema schema, IOptions<GraphQLContentOptions> optionsAccessor, IOptions<GraphQLSettings> settingsAccessor)
        {
            Name = "ContentItems";

            Type = typeof(ListGraphType<ContentItemType>);

            var whereInput = new ContentItemWhereInput(contentItemName, optionsAccessor);
            var orderByInput = new ContentItemOrderByInput(contentItemName);

            Arguments = new QueryArguments(
                new QueryArgument<ContentItemWhereInput> { Name = "where", Description = "filters the content items", ResolvedType = whereInput },
                new QueryArgument<ContentItemOrderByInput> { Name = "orderBy", Description = "sort order", ResolvedType = orderByInput },
                new QueryArgument<IntGraphType> { Name = "first", Description = "the first n content items", ResolvedType = new IntGraphType() },
                new QueryArgument<IntGraphType> { Name = "skip", Description = "the number of content items to skip", ResolvedType = new IntGraphType() },
                new QueryArgument<PublicationStatusGraphType> { Name = "status", Description = "publication status of the content item", ResolvedType = new PublicationStatusGraphType(), DefaultValue = PublicationStatusEnum.Published }
            );

            Resolver = new LockedAsyncFieldResolver<IEnumerable<ContentItem>>(Resolve);

            schema.RegisterType(whereInput);
            schema.RegisterType(orderByInput);
            schema.RegisterType<PublicationStatusGraphType>();

            _defaultNumberOfItems = settingsAccessor.Value.DefaultNumberOfResults;
        }

        private async Task<IEnumerable<ContentItem>> Resolve(IResolveFieldContext context)
        {
            var versionOption = VersionOptions.Published;

            if (context.HasPopulatedArgument("status"))
            {
                versionOption = GetVersionOption(context.GetArgument<PublicationStatusEnum>("status"));
            }

            JObject where = null;
            if (context.HasArgument("where"))
            {
                var whereArgument = context.Arguments["where"];

                if (whereArgument.Value != null)
                {
                    where = JObject.FromObject(whereArgument.Value);
                }
            }

            var session = context.RequestServices.GetService<ISession>();

            var preQuery = session.Query<ContentItem>();

            var filters = context.RequestServices.GetServices<IGraphQLFilter<ContentItem>>();

            foreach (var filter in filters)
            {
                preQuery = await filter.PreQueryAsync(preQuery, context);
            }

            var query = preQuery.With<ContentItemIndex>();

            query = FilterVersion(query, versionOption);
            query = FilterContentType(query, context);
            query = OrderBy(query, context);

            var contentItemsQuery = FilterWhereArguments(query, where, context, session);
            contentItemsQuery = PageQuery(contentItemsQuery, context);

            var contentItems = await contentItemsQuery.ListAsync();

            foreach (var filter in filters)
            {
                contentItems = await filter.PostQueryAsync(contentItems, context);
            }

            return contentItems;
        }

        private IQuery<ContentItem> FilterWhereArguments(
            IQuery<ContentItem, ContentItemIndex> query,
            JObject where,
            IResolveFieldContext fieldContext,
            ISession session)
        {
            if (where == null)
            {
                return query;
            }

            string defaultTableAlias = query.GetTypeAlias(typeof(ContentItemIndex));

            IPredicateQuery predicateQuery = new PredicateQuery(
                dialect: session.Store.Configuration.SqlDialect,
                shellSettings: fieldContext.RequestServices.GetService<ShellSettings>(),
                propertyProviders: fieldContext.RequestServices.GetServices<IIndexPropertyProvider>());

            // Create the default table alias
            predicateQuery.CreateAlias("", nameof(ContentItemIndex));
            predicateQuery.CreateTableAlias(nameof(ContentItemIndex), defaultTableAlias);

            // Add all provided table alias to the current predicate query
            var providers = fieldContext.RequestServices.GetServices<IIndexAliasProvider>();
            var indexes = new Dictionary<string, IndexAlias>(StringComparer.OrdinalIgnoreCase);
            var indexAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var aliasProvider in providers)
            {
                foreach (var alias in aliasProvider.GetAliases())
                {
                    predicateQuery.CreateAlias(alias.Alias, alias.Index);
                    indexAliases.Add(alias.Alias, alias.Alias);
                    if (!indexes.ContainsKey(alias.Index))
                    {
                        indexes.Add(alias.Index, alias);
                    }
                }
            }

            var expressions = Expression.Conjunction();
            BuildWhereExpressions(where, expressions, null, fieldContext, indexAliases);
            expressions.SearchUsedAlias(predicateQuery);

            // Add all Indexes that were used in the predicate query

            IQuery<ContentItem> contentQuery = query;
            foreach (var usedAlias in predicateQuery.GetUsedAliases())
            {
                if (indexes.ContainsKey(usedAlias))
                {
                    contentQuery = contentQuery.With(indexes[usedAlias].IndexType);
                    var tableAlias = query.GetTypeAlias(indexes[usedAlias].IndexType);
                    predicateQuery.CreateTableAlias(indexes[usedAlias].Index, tableAlias);
                }
            }

            var whereSqlClause = expressions.ToSqlString(predicateQuery);


            query = query.Where(whereSqlClause);

            // Add all parameters that were used in the predicate query
            foreach (var parameter in predicateQuery.Parameters)
            {
                query = query.WithParameter(parameter.Key, parameter.Value);
            }

            return contentQuery;
        }

        private IQuery<ContentItem> PageQuery(IQuery<ContentItem> contentItemsQuery, IResolveFieldContext context)
        {
            var first = context.GetArgument<int>("first");

            if (first == 0)
            {
                first = _defaultNumberOfItems;
            }

            contentItemsQuery = contentItemsQuery.Take(first);

            if (context.HasPopulatedArgument("skip"))
            {
                var skip = context.GetArgument<int>("skip");

                contentItemsQuery = contentItemsQuery.Skip(skip);
            }

            return contentItemsQuery;
        }

        private VersionOptions GetVersionOption(PublicationStatusEnum status)
        {
            switch (status)
            {
                case PublicationStatusEnum.Published: return VersionOptions.Published;
                case PublicationStatusEnum.Draft: return VersionOptions.Draft;
                case PublicationStatusEnum.Latest: return VersionOptions.Latest;
                case PublicationStatusEnum.All: return VersionOptions.AllVersions;
                default: return VersionOptions.Published;
            }
        }

        private static IQuery<ContentItem, ContentItemIndex> FilterContentType(IQuery<ContentItem, ContentItemIndex> query, IResolveFieldContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contentType = ((ListGraphType)(context.FieldDefinition).ResolvedType).ResolvedType.Name;
            return query.Where(q => q.ContentType == contentType);
        }

        private static IQuery<ContentItem, ContentItemIndex> FilterVersion(IQuery<ContentItem, ContentItemIndex> query, VersionOptions versionOption)
        {
            if (versionOption.IsPublished)
            {
                query = query.Where(q => q.Published == true);
            }
            else if (versionOption.IsDraft)
            {
                query = query.Where(q => q.Latest == true && q.Published == false);
            }
            else if (versionOption.IsLatest)
            {
                query = query.Where(q => q.Latest == true);
            }

            return query;
        }

        private void BuildWhereExpressions(JToken where, Junction expressions, string tableAlias, IResolveFieldContext fieldContext, IDictionary<string, string> indexAliases)
        {
            if (where is JArray array)
            {
                foreach (var child in array.Children())
                {
                    if (child is JObject whereObject)
                    {
                        BuildExpressionsInternal(whereObject, expressions, tableAlias, fieldContext, indexAliases);
                    }
                }
            }
            else if (where is JObject whereObject)
            {
                BuildExpressionsInternal(whereObject, expressions, tableAlias, fieldContext, indexAliases);
            }
        }

        private void BuildExpressionsInternal(JObject where, Junction expressions, string tableAlias, IResolveFieldContext fieldContext, IDictionary<string, string> indexAliases)
        {
            foreach (var entry in where.Properties())
            {
                IPredicate expression = null;

                var values = entry.Name.Split('_', 2);

                // Gets the full path name without the comparison e.g. aliasPart.alias, not aliasPart.alias_contains.
                var property = values[0];

                // figure out table aliases for collapsed parts and ones with the part suffix removed by the dsl
                if (tableAlias == null || !tableAlias.EndsWith("Part", StringComparison.OrdinalIgnoreCase))
                {
                    var whereArgument = fieldContext?.FieldDefinition.Arguments.FirstOrDefault(x => x.Name == "where");

                    if (whereArgument != null)
                    {
                        var whereInput = (WhereInputObjectGraphType)whereArgument.ResolvedType;

                        foreach (var field in whereInput.Fields.Where(x => x.GetMetadata<string>("PartName") != null))
                        {
                            var partName = field.GetMetadata<string>("PartName");
                            if ((tableAlias == null && field.GetMetadata<bool>("PartCollapsed") && field.Name.Equals(property, StringComparison.OrdinalIgnoreCase)) ||
                                (tableAlias != null && partName.ToFieldName().Equals(tableAlias, StringComparison.OrdinalIgnoreCase)))
                            {
                                tableAlias = indexAliases.TryGetValue(partName, out var indexTableAlias) ? indexTableAlias : tableAlias;
                                break;
                            }
                        }
                    }
                }

                if (tableAlias != null)
                {
                    property = $"{tableAlias}.{property}";
                }

                if (values.Length == 1)
                {
                    if (string.Equals(values[0], "or", StringComparison.OrdinalIgnoreCase))
                    {
                        expression = Expression.Disjunction();
                        BuildWhereExpressions(entry.Value, (Junction)expression, tableAlias, fieldContext, indexAliases);
                    }
                    else if (string.Equals(values[0], "and", StringComparison.OrdinalIgnoreCase))
                    {
                        expression = Expression.Conjunction();
                        BuildWhereExpressions(entry.Value, (Junction)expression, tableAlias, fieldContext, indexAliases);
                    }
                    else if (string.Equals(values[0], "not", StringComparison.OrdinalIgnoreCase))
                    {
                        expression = Expression.Conjunction();
                        BuildWhereExpressions(entry.Value, (Junction)expression, tableAlias, fieldContext, indexAliases);
                        expression = Expression.Not(expression);
                    }
                    else if (entry.HasValues && entry.Value.Type == JTokenType.Object)
                    {
                        // Loop through the part's properties, passing the name of the part as the table tableAlias.
                        // This tableAlias can then be used with the table alias to index mappings to join with the correct table.
                        BuildWhereExpressions(entry.Value, expressions, values[0], fieldContext, indexAliases);
                    }
                    else
                    {
                        var propertyValue = entry.Value.ToObject<object>();
                        expression = Expression.Equal(property, propertyValue);
                    }
                }
                else
                {
                    var value = entry.Value.ToObject<object>();

                    switch (values[1])
                    {
                        case "not": expression = Expression.Not(Expression.Equal(property, value)); break;
                        case "gt": expression = Expression.GreaterThan(property, value); break;
                        case "gte": expression = Expression.GreaterThanOrEqual(property, value); break;
                        case "lt": expression = Expression.LessThan(property, value); break;
                        case "lte": expression = Expression.LessThanOrEqual(property, value); break;
                        case "contains": expression = Expression.Like(property, (string)value, MatchOptions.Contains); break;
                        case "not_contains": expression = Expression.Not(Expression.Like(property, (string)value, MatchOptions.Contains)); break;
                        case "starts_with": expression = Expression.Like(property, (string)value, MatchOptions.StartsWith); break;
                        case "not_starts_with": expression = Expression.Not(Expression.Like(property, (string)value, MatchOptions.StartsWith)); break;
                        case "ends_with": expression = Expression.Like(property, (string)value, MatchOptions.EndsWith); break;
                        case "not_ends_with": expression = Expression.Not(Expression.Like(property, (string)value, MatchOptions.EndsWith)); break;
                        case "in": expression = Expression.In(property, entry.Value.ToObject<object[]>()); break;
                        case "not_in": expression = Expression.Not(Expression.In(property, entry.Value.ToObject<object[]>())); break;

                        default: expression = Expression.Equal(property, value); break;
                    }
                }

                if (expression != null)
                {
                    expressions.Add(expression);
                }
            }
        }

        private IQuery<ContentItem, ContentItemIndex> OrderBy(IQuery<ContentItem, ContentItemIndex> query,
            IResolveFieldContext context)
        {
            if (context.HasPopulatedArgument("orderBy"))
            {
                var orderByArguments = JObject.FromObject(context.Arguments["orderBy"].Value);

                if (orderByArguments != null)
                {
                    var thenBy = false;

                    foreach (var property in orderByArguments.Properties())
                    {
                        var direction = (OrderByDirection)property.Value.Value<int>();

                        Expression<Func<ContentItemIndex, object>> selector = null;

                        switch (property.Name)
                        {
                            case "contentItemId": selector = x => x.ContentItemId; break;
                            case "contentItemVersionId": selector = x => x.ContentItemVersionId; break;
                            case "displayText": selector = x => x.DisplayText; break;
                            case "published": selector = x => x.Published; break;
                            case "latest": selector = x => x.Latest; break;
                            case "createdUtc": selector = x => x.CreatedUtc; break;
                            case "modifiedUtc": selector = x => x.ModifiedUtc; break;
                            case "publishedUtc": selector = x => x.PublishedUtc; break;
                            case "owner": selector = x => x.Owner; break;
                            case "author": selector = x => x.Author; break;
                        }

                        if (selector != null)
                        {
                            if (!thenBy)
                            {
                                query = direction == OrderByDirection.Ascending
                                        ? query.OrderBy(selector)
                                        : query.OrderByDescending(selector)
                                    ;
                            }
                            else
                            {
                                query = direction == OrderByDirection.Ascending
                                        ? query.ThenBy(selector)
                                        : query.ThenByDescending(selector)
                                    ;
                            }

                            thenBy = true;
                        }
                    }
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedUtc);
            }

            return query;
        }
    }
}
