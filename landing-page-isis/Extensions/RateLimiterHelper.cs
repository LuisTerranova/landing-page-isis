using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace landing_page_isis.Extensions;

public static class RateLimiterHelper
{
    private static readonly ConcurrentDictionary<string, FixedWindowRateLimiter> _limiters = new();

    public static void Reset()
    {
        foreach (var kv in _limiters)
            kv.Value.Dispose();
        _limiters.Clear();
    }

    public static async Task<bool> CheckAsync(string key, int limit, TimeSpan window)
    {
        var limiter = _limiters.GetOrAdd(key, _ =>
            new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = window,
                QueueLimit = 0,
            })
        );

        using var lease = await limiter.AcquireAsync();
        return lease.IsAcquired;
    }
}
