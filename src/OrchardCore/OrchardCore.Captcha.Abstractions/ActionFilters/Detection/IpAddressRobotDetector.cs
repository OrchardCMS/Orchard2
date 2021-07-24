using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrchardCore.Captcha.Configuration;

namespace OrchardCore.Captcha.ActionFilters.Detection
{
     public class IpAddressRobotDetector : IDetectRobots
    {
        private const string IpAddressAbuseDetectorCacheKey = "IpAddressRobotDetector";

        private readonly IMemoryCache _memoryCache;
        private readonly HttpContext _httpContext;
        private readonly CaptchaSettings _settings;

        public IpAddressRobotDetector(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, IOptions<CaptchaSettings> settingsAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _memoryCache = memoryCache;
            _settings = settingsAccessor.Value;
        }

        public void IsNotARobot()
        {
            var ipAddressKey = GetIpAddressCacheKey();
            _memoryCache.Remove(ipAddressKey);
        }

        private string GetIpAddressCacheKey()
        {
            return $"{IpAddressAbuseDetectorCacheKey}:{GetIpAddress()}";
        }

        private string GetIpAddress()
        {
            return _httpContext.Connection.RemoteIpAddress.ToString();
        }

        public RobotDetectionResult DetectRobot()
        {
            var ipAddressKey = GetIpAddressCacheKey();
            var faultyRequestCount = _memoryCache.GetOrCreate(ipAddressKey, fact => 0);

            return new RobotDetectionResult()
            {
                IsRobot = faultyRequestCount > _settings.DetectionThreshold
            };
        }

        public void FlagAsRobot()
        {
            var ipAddressKey = GetIpAddressCacheKey();

            // this has race conditions, but it's ok
            var faultyRequestCount = _memoryCache.GetOrCreate(ipAddressKey, fact => 0);
            faultyRequestCount++;
            _memoryCache.Set(ipAddressKey, faultyRequestCount);
        }
    }
}
