using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace UserPermissionSystem.Infrastructure.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        // 用于跟踪缓存键，以便实现按前缀删除和清除所有缓存
        private readonly ConcurrentDictionary<string, bool> _cacheKeys = new();

        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expireTime = 30)
        {
            try
            {
                if (_memoryCache.TryGetValue<T>(key, out var value))
                {
                    _logger.LogDebug("从缓存获取数据: {Key}", key);
                    return value;
                }

                value = await factory();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(expireTime))
                    .RegisterPostEvictionCallback((_, _, _, _) =>
                    {
                        _cacheKeys.TryRemove(key, out _);
                    });

                _memoryCache.Set(key, value, cacheOptions);
                _cacheKeys[key] = true;
                _logger.LogDebug("缓存数据: {Key}，过期时间: {ExpireTime}分钟", key, expireTime);

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取或创建缓存时发生错误: {Key}", key);
                return await factory();
            }
        }

        public Task<T> GetAsync<T>(string key)
        {
            _logger.LogDebug("尝试获取缓存: {Key}", key);
            _memoryCache.TryGetValue<T>(key, out var value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, int expireTime = 30)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(expireTime))
                .RegisterPostEvictionCallback((_, _, _, _) =>
                {
                    _cacheKeys.TryRemove(key, out _);
                });

            _memoryCache.Set(key, value, cacheOptions);
            _cacheKeys[key] = true;
            _logger.LogDebug("设置缓存: {Key}，过期时间: {ExpireTime}分钟", key, expireTime);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogDebug("移除缓存: {Key}", key);
            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            foreach (var key in _cacheKeys.Keys.ToList())
            {
                _memoryCache.Remove(key);
            }
            _cacheKeys.Clear();
            _logger.LogWarning("清除所有缓存");
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            var keysToRemove = _cacheKeys.Keys.Where(k => k.StartsWith(prefix)).ToList();
            
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }
            
            _logger.LogDebug("按前缀移除缓存: {Prefix}, 移除了{Count}项", prefix, keysToRemove.Count);
            return Task.CompletedTask;
        }
    }
}