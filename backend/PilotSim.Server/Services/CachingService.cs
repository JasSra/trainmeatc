using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PilotSim.Server.Services;

public interface ICachingService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

public class CachingService : ICachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<CachingService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CachingService(
        IMemoryCache memoryCache, 
        IDistributedCache? distributedCache,
        ILogger<CachingService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try memory cache first (fastest)
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return memoryValue;
            }

            // Try distributed cache if available
            if (_distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (distributedValue != null)
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                    
                    // Store in memory cache for faster access
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    
                    _logger.LogDebug("Cache hit (distributed): {Key}", key);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var exp = expiration ?? TimeSpan.FromMinutes(30);

            // Set in memory cache
            _memoryCache.Set(key, value, exp);

            // Set in distributed cache if available
            if (_distributedCache != null)
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = exp
                };
                await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            }

            _logger.LogDebug("Cache set: {Key} (expires in {Expiration})", key, exp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to cache: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);

            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }

            _logger.LogDebug("Cache remove: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from cache: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based removal is complex with distributed cache
        // For now, we'll just log this operation
        _logger.LogInformation("Pattern-based cache removal requested: {Pattern}", pattern);
        
        // Memory cache doesn't support pattern removal directly
        // This would require a more sophisticated implementation
        await Task.CompletedTask;
    }
}