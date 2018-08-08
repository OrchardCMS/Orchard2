using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace OrchardCore.ReCaptcha.Core.ActionFilters.Abuse
{
    public class IpAddressAbuseDetector : IDetectAbuse
    {
        private const string IpAddressAbuseDetectorCacheKey = "IpAddressAbuseDetector";

        private readonly IMemoryCache _memoryCache;

        public IpAddressAbuseDetector(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void ClearAbuseFlags(HttpContext context)
        {
            var ipAddressKey = GetIpAddressCacheKey(context);
            _memoryCache.Remove(ipAddressKey);
        }

        private string GetIpAddressCacheKey(HttpContext context)
        {
            return $"{IpAddressAbuseDetectorCacheKey}:{GetIpAddress(context)}";
        }

        private string GetIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress.ToString();
        }

        public AbuseDetectResult DetectAbuse(HttpContext context)
        {
            var ipAddressKey = GetIpAddressCacheKey(context);
            var faultyRequestCount = _memoryCache.GetOrCreate<int>(ipAddressKey, fact => 0);

            return new AbuseDetectResult()
            {
                // this should be configurable
                SuspectAbuse = faultyRequestCount > 5
            };
        }

        public void FlagPossibleAbuse(HttpContext context)
        {
            var ipAddressKey = GetIpAddressCacheKey(context);
            
            // this has race conditions, but it's ok
            var faultyRequestCount = _memoryCache.GetOrCreate<int>(ipAddressKey, fact => 0);
            faultyRequestCount++;
            _memoryCache.Set(ipAddressKey, faultyRequestCount);
        }
    }
}
