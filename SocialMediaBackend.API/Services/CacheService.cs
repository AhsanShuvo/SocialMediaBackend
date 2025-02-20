using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Settings;
using StackExchange.Redis;

namespace SocialMediaBackend.API.Services
{
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _cache;
        private readonly RedisSettings _settings;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IConnectionMultiplexer redis, IOptions<RedisSettings> options, ILogger<CacheService> logger)
        {
            _redis = redis;
            _settings = options.Value;
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var jsonData = JsonConvert.SerializeObject(value);
            await _cache.StringSetAsync(key, jsonData, expiration ?? TimeSpan.FromMinutes(_settings.Cache_TTL));
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var jsonData = await _cache.StringGetAsync(key);

                if (!jsonData.HasValue)
                {
                    return default;
                }
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to fetch data from cache for key: {key}", key);

                return default;
            }

        }

        public async Task RemoveAsync(string prefix)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Any())
            {
                await _cache.KeyDeleteAsync(keys);
                Console.WriteLine($"Deleted {keys.Length} keys with prefix: {prefix}");
            }
            else
            {
                Console.WriteLine($"No keys found with prefix: {prefix}");
            }
        }
    }
}
