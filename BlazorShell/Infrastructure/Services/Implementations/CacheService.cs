using System;
using System.Threading.Tasks;
using BlazorShell.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            return Task.FromResult(_cache.Get<T>(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key {Key}", key);
            return Task.FromResult(default(T));
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                cacheOptions.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            }

            _cache.Set(key, value, cacheOptions);
            _logger.LogDebug("Cache set for key {Key}", key);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key {Key}", key);
            throw;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Cache removed for key {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key {Key}", key);
            throw;
        }
    }

    public Task ClearAsync()
    {
        try
        {
            _logger.LogInformation("Cache clear requested");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }
}
