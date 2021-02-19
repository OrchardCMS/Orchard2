using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.DisplayManagement;
using OrchardCore.Secrets;
using OrchardCore.Workflows.Http.Controllers;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Http.Services
{
    public class HttpRequestEventSecretService : IHttpRequestEventSecretService
    {
        private const string TokenCacheKeyPrefix = "HttpRequestEventToken:";
        private readonly IMemoryCache _memoryCache;
        private readonly ISecretService<HttpRequestEventSecret> _secretService;
        private readonly ISecurityTokenService _securityTokenService;
        private readonly ViewContextAccessor _viewContextAccessor;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public HttpRequestEventSecretService(
            IMemoryCache memoryCache,
            ISecretService<HttpRequestEventSecret> secretService,
            ISecurityTokenService securityTokenService,
            ViewContextAccessor viewContextAccessor,
            IUrlHelperFactory urlHelperFactory)
        {
            _memoryCache = memoryCache;
            _secretService = secretService;
            _securityTokenService = securityTokenService;
            _viewContextAccessor = viewContextAccessor;
            _urlHelperFactory = urlHelperFactory;
        }

        public async Task<string> GetUrlAsync(string httpRequestEventSecretName)
        {
            var secret = await _secretService.GetSecretAsync(httpRequestEventSecretName);
            if (secret == null)
            {
                return null;
            }

            var tokenLifeSpan = secret.TokenLifeSpan == 0 ? HttpWorkflowController.NoExpiryTokenLifespan : secret.TokenLifeSpan;

            // The cache key is a representation of the secrets values.
            // If the secrets value changes the cache key will no longer be valid, and expire automatically.
            var cacheKey = $"{TokenCacheKeyPrefix}{secret.WorkflowTypeId}{secret.ActivityId}{tokenLifeSpan.ToString()}";

            var url = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var urlHelper = _urlHelperFactory.GetUrlHelper(_viewContextAccessor.ViewContext);
                var token = _securityTokenService.CreateToken(new WorkflowPayload(secret.WorkflowTypeId, secret.ActivityId), TimeSpan.FromDays(tokenLifeSpan));

                return urlHelper.Action("Invoke", "HttpWorkflow", new { area = "OrchardCore.Workflows", token = token });
            });

            return url;
        }
    }
}
