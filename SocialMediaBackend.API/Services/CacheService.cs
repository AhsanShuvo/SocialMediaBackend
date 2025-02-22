using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
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
        private const string SortedSetKey = "post_sorted";

        public CacheService(IConnectionMultiplexer redis, IOptions<RedisSettings> options, ILogger<CacheService> logger)
        {
            _redis = redis;
            _settings = options.Value;
            _logger = logger;
            _cache = redis.GetDatabase();
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

        public async Task AddPostAsync(string postId, long timestamp, Post postContent)
        {
            await _cache.SortedSetAddAsync(SortedSetKey, postId, timestamp);

            string postKey = $"post:{postId}";
            await _cache.StringSetAsync(postKey, JsonConvert.SerializeObject(postContent));
            await InvalidateOldData();
        }

        public async Task<List<string>> GetPaginatedPostsAsync(long? cursor, int limit)
        {
            long startTimestamp = cursor ?? long.MaxValue;

            var posts = await _cache.SortedSetRangeByScoreAsync(
                SortedSetKey,
                double.NegativeInfinity,
                startTimestamp,           
                Exclude.None,
                Order.Descending,
                0, limit
            );
            return posts.Select(p => p.ToString()).ToList();
        }

        public async Task<Post?> GetPostByIdAsync(string postId)
        {
            string postKey = $"post:{postId}";
            var jsonData = await _cache.StringGetAsync(postKey);

            if(!jsonData.HasValue)
            {
                return default;
            }
            return JsonConvert.DeserializeObject<Post>(jsonData);
        }

        public async Task AddCommentToPostAsync(string postId, string commentId, Comment comment)
        {
            string commentKey = $"post:{postId}:latest_comments";

            await _cache.ListLeftPushAsync(commentKey, JsonConvert.SerializeObject(comment));
            await _cache.ListTrimAsync(commentKey, 0, 5);
        }

        public async Task<List<Comment>> GetLatestCommentsAsync(string postId)
        {
            string commentKey = $"post:{postId}:latest_comments";
            var jsonComments = await _cache.ListRangeAsync(commentKey, 0, 1);
            var jsonData = jsonComments.Select(c => c.ToString()).ToList();

            var comments = jsonData.Select(json => JsonConvert.DeserializeObject<Comment>(json)).ToList();
            if (comments.Any())
            {
                return comments;
            }
            return new List<Comment>();
        }

        public async Task DeletePostAsync(string postId)
        {
            bool postRemoved = await _cache.SortedSetRemoveAsync(SortedSetKey, postId);

            string postKey = $"post:{postId}";
            await _cache.KeyDeleteAsync(postKey);

            string commentKey = $"post:{postId}:latest_comments";
            await _cache.KeyDeleteAsync(commentKey);
        }

        public async Task DeleteCommentAsync(string postId, string commentId)
        {
            string commentKey = $"post:{postId}:latest_comments";

            var comments = await _cache.ListRangeAsync(commentKey, 0, -1);

            var commentToRemove = comments.FirstOrDefault(c => c.ToString().Contains(commentId));
            if (!commentToRemove.HasValue) return;

            long removedCount = await _cache.ListRemoveAsync(commentKey, commentToRemove);
        }

        private async Task InvalidateOldData()
        {
            long postCount = await _cache.SortedSetLengthAsync(SortedSetKey);
            if (postCount > _settings.MaxSize)
            {
                var oldestPosts = await _cache.SortedSetRangeByRankAsync(SortedSetKey, 0, postCount - _settings.MaxSize - 1);
                if (oldestPosts.Length > 0)
                {
                    await _cache.SortedSetRemoveAsync(SortedSetKey, oldestPosts);

                    var deleteOldData = oldestPosts.Select(async p =>
                    {
                        var key = $"post:{p.ToString()}";
                        await _cache.KeyDeleteAsync(key);

                        string commentKey = $"post:{p.ToString()}:latest_comments";
                        await _cache.KeyDeleteAsync(commentKey);
                    });
                    await Task.WhenAll(deleteOldData);
                }
            }
        }
    }
}
