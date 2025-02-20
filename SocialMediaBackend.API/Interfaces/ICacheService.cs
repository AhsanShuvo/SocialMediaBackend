using StackExchange.Redis;

namespace SocialMediaBackend.API.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string prefix);
    }
}
