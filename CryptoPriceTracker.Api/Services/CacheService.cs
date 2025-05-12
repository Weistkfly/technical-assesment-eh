using Microsoft.Extensions.Caching.Memory;

namespace CryptoPriceTracker.Api.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _cache;
        public CacheService(IMemoryCache memoryCache)
        {
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<T?> GetOrCreateAsync<T>(
            string cacheKey,
         Func<CancellationToken, Task<T?>> getFromDatabaseAsync,
         CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

                return getFromDatabaseAsync(cancellationToken);
            });
        }
    }
}