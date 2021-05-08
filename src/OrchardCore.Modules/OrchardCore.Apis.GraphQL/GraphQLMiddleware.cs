using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Apis.GraphQL.Queries;
using OrchardCore.Apis.GraphQL.ValidationRules;
using OrchardCore.Routing;

namespace OrchardCore.Apis.GraphQL
{
    public class GraphQLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLSettings _settings;
        private readonly IDocumentExecuter _executer;

        internal static readonly Encoding _utf8Encoding = new UTF8Encoding(false);
        private readonly static MediaType _jsonMediaType = new MediaType("application/json");
        private readonly static MediaType _graphQlMediaType = new MediaType("application/graphql");

        public GraphQLMiddleware(
            RequestDelegate next,
            GraphQLSettings settings,
            IDocumentExecuter executer)
        {
            _next = next;
            _settings = settings;
            _executer = executer;
        }

        public async Task Invoke(HttpContext context, IAuthorizationService authorizationService, IAuthenticationService authenticationService, ISchemaFactory schemaService, IDocumentWriter documentWriter)
        {
            if (!IsGraphQLRequest(context))
            {
                await _next(context);
            }
            else
            {
                var authenticateResult = await authenticationService.AuthenticateAsync(context, "Api");

                if (authenticateResult.Succeeded)
                {
                    context.User = authenticateResult.Principal;
                }

                var authorized = await authorizationService.AuthorizeAsync(context.User, Permissions.ExecuteGraphQL);

                if (authorized)
                {
                    await ExecuteAsync(context, schemaService, documentWriter);
                }
                else
                {
                    await context.ChallengeAsync("Api");
                }
            }
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithNormalizedSegments(_settings.Path, StringComparison.OrdinalIgnoreCase);
        }

        private async Task ExecuteAsync(HttpContext context, ISchemaFactory schemaService, IDocumentWriter documentWriter)
        {
           

            GraphQLRequest request = null;

            // c.f. https://graphql.org/learn/serving-over-http/#post-request

            try
            {
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    var mediaType = new MediaType(context.Request.ContentType);

                    if (mediaType.IsSubsetOf(_jsonMediaType) || mediaType.IsSubsetOf(_graphQlMediaType))
                    {
                        using var sr = new StreamReader(context.Request.Body);

                        if (mediaType.IsSubsetOf(_graphQlMediaType))
                        {
                            request = new GraphQLRequest
                            {
                                Query = await sr.ReadToEndAsync()
                            };
                        }
                        else
                        {
                            var json = await sr.ReadToEndAsync();
                            request = JObject.Parse(json).ToObject<GraphQLRequest>();
                        }
                    }
                    else
                    {
                        request = CreateRequestFromQueryString(context);
                    }
                }
                else if (HttpMethods.IsGet(context.Request.Method))
                {
                    request = CreateRequestFromQueryString(context, true);
                }

                if (request == null)
                {
                    throw new InvalidOperationException("Unable to create a graphqlrequest from this request");
                }
            }
            catch (Exception e)
            {
                await documentWriter.WriteErrorAsync(context, "An error occurred while processing the GraphQL query", e);
                return;
            }

            var queryToExecute = request.Query;

            if (!String.IsNullOrEmpty(request.NamedQuery))
            {
                var namedQueries = context.RequestServices.GetServices<INamedQueryProvider>();

                var queries = namedQueries
                    .SelectMany(dict => dict.Resolve())
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                queryToExecute = queries[request.NamedQuery];
            }

            var schema = await schemaService.GetSchemaAsync();
            var dataLoaderDocumentListener = context.RequestServices.GetRequiredService<IDocumentExecutionListener>();

            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = queryToExecute;
                _.OperationName = request.OperationName;
                _.Inputs = request.Variables.ToInputs();
                _.UserContext = _settings.BuildUserContext?.Invoke(context);
                _.RequestServices = context.RequestServices;
                _.ValidationRules = DocumentValidator.CoreRules
                                    .Concat(context.RequestServices.GetServices<IValidationRule>());
                _.ComplexityConfiguration = new ComplexityConfiguration
                {
                    MaxDepth = _settings.MaxDepth,
                    MaxComplexity = _settings.MaxComplexity,
                    FieldImpact = _settings.FieldImpact
                };
                _.Listeners.Add(dataLoaderDocumentListener);
            });

            context.Response.StatusCode = (int)(result.Errors == null || result.Errors.Count == 0
                ? HttpStatusCode.OK
                : result.Errors.Any(x => x.Code == RequiresPermissionValidationRule.ErrorCode)
                    ? HttpStatusCode.Unauthorized
                    : HttpStatusCode.BadRequest);

            context.Response.ContentType = MediaTypeNames.Application.Json;
            await documentWriter.WriteAsync(context.Response.Body, result);
        }

        private static GraphQLRequest CreateRequestFromQueryString(HttpContext context, bool validateQueryKey = false)
        {
            if (!context.Request.Query.ContainsKey("query"))
            {
                if (validateQueryKey)
                {
                    throw new InvalidOperationException("The 'query' query string parameter is missing");
                }

                return null;
            }

            var request = new GraphQLRequest
            {
                Query = context.Request.Query["query"]
            };

            if (context.Request.Query.ContainsKey("variables"))
            {
                request.Variables = JObject.Parse(context.Request.Query["variables"]);
            }

            if (context.Request.Query.ContainsKey("operationName"))
            {
                request.OperationName = context.Request.Query["operationName"];
            }

            return request;
        }
    }
}
