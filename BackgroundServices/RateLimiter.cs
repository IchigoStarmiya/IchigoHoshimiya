using System.Collections.Concurrent;

namespace IchigoHoshimiya.BackgroundServices;


public class RateLimiter
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
    
    public async Task WaitAsync(string key, int minDelayMs)
    {
        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync();
        try
        {
            if (_lastRequestTimes.TryGetValue(key, out var lastTime))
            {
                var elapsed = DateTime.UtcNow - lastTime;
                var remaining = TimeSpan.FromMilliseconds(minDelayMs) - elapsed;
                
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining);
                }
            }
            
            _lastRequestTimes[key] = DateTime.UtcNow;
        }
        finally
        {
            semaphore.Release();
        }
    }
}