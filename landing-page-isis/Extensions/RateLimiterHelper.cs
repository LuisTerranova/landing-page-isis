using Microsoft.Extensions.Caching.Memory;

namespace landing_page_isis.Extensions;

/// <summary>
/// Helper class for rate limiting operations.
/// </summary>
public static class RateLimiterHelper
{
    private static MemoryCache _cache = new(new MemoryCacheOptions());
    private static readonly object _lock = new();

    private class RateLimitCounter
    {
        public int Count { get; set; }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _cache.Dispose();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }
    }

    public static Task<bool> CheckAsync(string key, int limit, TimeSpan window)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out RateLimitCounter? counter) || counter == null)
            {
                counter = new RateLimitCounter { Count = 1 };
                _cache.Set(
                    key,
                    counter,
                    new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = window }
                );
                return Task.FromResult(true);
            }

            if (counter.Count >= limit)
            {
                return Task.FromResult(false);
            }

            counter.Count++;
            return Task.FromResult(true);
        }
    }
}
