using Newtonsoft.Json;
using StackExchange.Redis;

namespace SocialMediaBackend.API.Services
{
    public class CacheService
    {
        private readonly IDatabase _cache;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

        public CacheService(IConnectionMultiplexer redis)
        {
            _cache = redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var jsonData = JsonConvert.SerializeObject(value);
            await _cache.StringSetAsync(key, jsonData, expiration ?? _defaultExpiration);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var jsonData = await _cache.StringGetAsync(key);
            return jsonData.IsNullOrEmpty ? default : JsonConvert.DeserializeObject<T>(jsonData);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.KeyDeleteAsync(key);
        }
    }
}
