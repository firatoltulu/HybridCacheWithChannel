namespace HybridCacheExample
{
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Newtonsoft.Json;
    using StackExchange.Redis;
    using System;

    public class HybridCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ISubscriber _redisSubscriber;

        public HybridCache(IMemoryCache memoryCache, IDistributedCache distributedCache, ConnectionMultiplexer redisConnection)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _redisSubscriber = redisConnection.GetSubscriber();

            _redisSubscriber.Subscribe("cache_invalidation_channel", (channel, key) =>
            {
                _memoryCache.Remove(key);
            });
        }

        public void Set<T>(string key, T value, TimeSpan cacheTime)
        {
            _memoryCache.Set(key, value, cacheTime);

            var redisValue = JsonConvert.SerializeObject(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheTime
            };
            _distributedCache.SetString(key, redisValue, options);
        }

        public T Get<T>(string key)
        {
            if (_memoryCache.TryGetValue(key, out T value))
            {
                return value;
            }

            var redisValue = _distributedCache.GetString(key);
            if (redisValue != null)
            {
                value = JsonConvert.DeserializeObject<T>(redisValue);
                _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
                return value;
            }

            return default;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _distributedCache.Remove(key);
            _redisSubscriber.Publish("cache_invalidation_channel", key);
        }
    }

}
