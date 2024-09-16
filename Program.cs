using HybridCacheExample;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

var redisConnection = ConnectionMultiplexer.Connect("localhost");

var memoryCache = new MemoryCache(new MemoryCacheOptions());
var distributedCache = new RedisCache(new RedisCacheOptions
{
    Configuration = "localhost",
    InstanceName = "SampleInstance"
});

var hybridCache = new HybridCache(memoryCache, distributedCache, redisConnection);

hybridCache.Set("user_firat", new { Name = "Firat", Age = 40 }, TimeSpan.FromMinutes(30));

var user = hybridCache.Get<dynamic>("user_firat");
Console.WriteLine(user.Name);

hybridCache.Remove("user_firat");
