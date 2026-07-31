using System.Collections.Concurrent;

namespace Kromer.Jobs;

/// <summary>
/// Exponential rate-limiter.
/// </summary>
public class RateLimitService(IConfiguration configuration)
{
    private readonly TimeSpan _startingTime = TimeSpan.FromMilliseconds(configuration.GetValue("RateLimit:StartingTime", 1000));
    private readonly ConcurrentDictionary<string, RateLimitState> _addresses = new();
    private record RateLimitState(TimeSpan Time, DateTime LastTriggered);
    
    /// <summary>
    /// Check whether the address is currently rate-limited.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public bool IsLimited(string address)
    {
        if (!_addresses.TryGetValue(address, out var state))
        {
            return false;
        }
        
        var now = DateTime.UtcNow;

        return state.LastTriggered + state.Time - now > TimeSpan.Zero;
    }

    /// <summary>
    /// Process and act on the address.
    /// If the address is rate-limited, return false.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <returns>Whether the address is currently rate-limited.</returns>
    public bool Trigger(string address)
    {
        if (IsLimited(address))
        {
            return false;
        }
        
        
        
        return false;
    }
}