using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Search.Elastic.Model;

namespace OrchardCore.Search.Elastic.Controllers
{
    [Route("api/elasticsearch")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    public class ApiController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ElasticQuerySource _elasticQuerySource;

        public ApiController(
            IAuthorizationService authorizationService,
            ElasticQuerySource elasticQuerySource)
        {
            _authorizationService = authorizationService;
            _elasticQuerySource = elasticQuerySource;
        }

        [HttpGet]
        [Route("content")]
        public async Task<IActionResult> Content([FromQuery] ElasticQueryModel queryModel)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.QueryElasticApi))
            {
                return this.ChallengeOrForbid("Api");
            }

            var result = await ElasticQueryApiAsync(queryModel, returnContentItems: true);

            return new ObjectResult(result);
        }
        
        [HttpPost]
        [Route("content")]
        public async Task<IActionResult> ContentPost(ElasticQueryModel queryModel)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.QueryElasticApi))
            {
                return this.ChallengeOrForbid();
            }

            var result = await ElasticQueryApiAsync(queryModel, returnContentItems: true);

            return new ObjectResult(result);
        }

        [HttpGet]
        [Route("documents")]
        public async Task<IActionResult> Documents([FromQuery] ElasticQueryModel queryModel)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.QueryElasticApi))
            {
                return this.ChallengeOrForbid();
            } 

            var result = await ElasticQueryApiAsync(queryModel);

            return new ObjectResult(result);
        }

        [HttpPost]
        [Route("documents")]
        public async Task<IActionResult> DocumentsPost(ElasticQueryModel queryModel)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.QueryElasticApi))
            {
                return this.ChallengeOrForbid("Api");
            }

            var result = await ElasticQueryApiAsync(queryModel);

            return new ObjectResult(result);
        }

        private Task<Queries.IQueryResults> ElasticQueryApiAsync(ElasticQueryModel queryModel, bool returnContentItems = false)
        {
            var elasticQuery = new ElasticQuery
            {
                Index = queryModel.IndexName,
                Template = queryModel.Query,
                ReturnContentItems = returnContentItems
            };

            var queryParameters = queryModel.Parameters != null ?
                JsonConvert.DeserializeObject<Dictionary<string, object>>(queryModel.Parameters)
                : new Dictionary<string, object>();

            var result = _elasticQuerySource.ExecuteQueryAsync(elasticQuery, queryParameters);
            return result;
        }
    }
}
