using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Services
{
    public class PostService : IPostService
    {
        private readonly ILogger<PostService> _logger;
        private readonly CacheService _cacheService;
        private readonly AppDbContext _context;

        public PostService(ILogger<PostService> logger, CacheService cacheService, AppDbContext appDbContext)
        {
            _logger = logger;
            _cacheService = cacheService;
            _context = appDbContext;
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync(int limit, string? cursor)
        {
            string cacheKey = $"posts:cursor:{cursor}:limit:{limit}";

            var cachedData = await _cacheService.GetAsync<List<Post>>(cacheKey);

            if (cachedData is not null) return cachedData;

            var query = _context.Posts.Include(p => p.Creator).OrderByDescending(p => p.Comments.Count);

            if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
            {
                query = (IOrderedQueryable<Post>)query.Where(p => p.Id < cursorId); // need to fix
            }

            var posts = await query.Take(limit).ToListAsync();


            posts.ForEach(post =>
            {
                var processedImageUrl = post.ImageUrl.Replace("original-images", "processed-url") + ".jpg";
                post.ImageUrl = processedImageUrl;
                post.Comments = post.Comments.OrderBy(c => c.CreatedAt).Take(2).ToList();
            });

            await _cacheService.SetAsync(cacheKey, JsonConvert.SerializeObject(posts), TimeSpan.FromMinutes(10));
            return posts;
        }

        public async Task<Post> CreatePostAsync(CreatePostRequest reqeust)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Caption = reqeust.Caption,
                ImageUrl = reqeust.ImageUrl,
                CreatorId = reqeust.CreatorId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync("posts:*");
            return post;
        }
    }
}
