using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using OrchardCore.Environment.Cache;
using OrchardCore.Modules;
using YesSql;


namespace OrchardCore.Profile.Service
{
    public class ProfileService : IProfileService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISignal _signal;
        private readonly IClock _clock;
        private const string ProfileCacheKey = "ProfileService";

        public ProfileService(
            ISignal signal,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor,
            IClock clock)
        {
            _signal = signal;
            _clock = clock;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public IChangeToken ChangeToken => _signal.GetToken(ProfileCacheKey);

        public async Task<IProfile> GetProfileAsync()
        {
            IProfile profile;

            if (!_memoryCache.TryGetValue(ProfileCacheKey, out profile))
            {
                var session = GetSession();

                profile = await session.Query<Profile>().FirstOrDefaultAsync();

                if (profile == null)
                {
                    lock (_memoryCache)
                    {
                        if (!_memoryCache.TryGetValue(ProfileCacheKey, out profile))
                        {
                            profile = new Profile
                            {
                                UserName = "Admin"
                            };

                            session.Save(profile);
                            _memoryCache.Set(ProfileCacheKey, profile);
                            _signal.SignalToken(ProfileCacheKey);
                        }
                    }
                }
                else
                {
                    _memoryCache.Set(ProfileCacheKey, profile);
                    _signal.SignalToken(ProfileCacheKey);
                }
            }

            return profile;
        }

        /// <inheritdoc/>
        public async Task UpdateProfileAsync(IProfile profile)
        {
            var session = GetSession();

            var existing = await session.Query<Profile>().FirstOrDefaultAsync();

            existing.UserName = profile.UserName;
            session.Save(existing);

            _memoryCache.Set(ProfileCacheKey, profile);
            _signal.SignalToken(ProfileCacheKey);

            return;
        }

        private YesSql.ISession GetSession()
        {
            return _httpContextAccessor.HttpContext.RequestServices.GetService<YesSql.ISession>();
        }
    }
}
