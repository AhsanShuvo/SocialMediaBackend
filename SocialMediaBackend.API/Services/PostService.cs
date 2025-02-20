using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Settings;
using System.Text;

namespace SocialMediaBackend.API.Services
{
    public class PostService : IPostService
    {
        private readonly ILogger<PostService> _logger;
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _context;
        private readonly AzureStorageSettings _settings;

        public PostService(ILogger<PostService> logger, ICacheService cacheService, AppDbContext appDbContext, IOptions<AzureStorageSettings> options)
        {
            _logger = logger;
            _cacheService = cacheService;
            _context = appDbContext;
            _settings = options.Value;
        }

        public async Task<PaginatedPostResponse> GetAllPostsAsync(int limit, string? cursor)
        {
            string cacheKey = $"posts:cursor:{cursor}:limit:{limit}";

            var cachedData = await _cacheService.GetAsync<PaginatedPostResponse>(cacheKey);

            if (cachedData is not null) return cachedData;

            var lastPostTimeStamp = DecodeCursor(cursor);


            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.CreatedAt < lastPostTimeStamp)
                .OrderByDescending(p => p.Comments.Count)
                .Take(limit)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    CreatedAt = p.CreatedAt,
                    ImageUrl = p.ImageUrl,
                    Creator = new CreatorDto
                    {
                        Id = p.Creator.Id,
                        Name = p.Creator.Name
                    },
                    RecentComments = p.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(2)
                        .Select(c => new CommentDto
                        {
                            Id = c.Id,
                            Content = c.Content,
                            CreatorDto = new CreatorDto
                            {
                                Id = c.CreatorId,
                                Name = c.Creator.Name,
                            },
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                })
                .ToListAsync();

            var nextCursor = posts.Count() > 0 ? GenerateCursor(posts.Last().CreatedAt) : GenerateCursor(lastPostTimeStamp);

            posts.ForEach(p =>
            {
                p.ImageUrl = p.ImageUrl.Replace(_settings.OriginalContainerName, _settings.ProcessedContainerName);
            });

            var paginatedPosts = new PaginatedPostResponse
            {
                Posts = posts,
                NextPageToken = nextCursor
            };
            await _cacheService.SetAsync(cacheKey, paginatedPosts);

            return paginatedPosts;
        }

        public async Task<bool> CreatePostAsync(CreatePostRequest reqeust)
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
            return true;
        }

        private DateTime DecodeCursor(string? cursor)
        {
            if (string.IsNullOrEmpty(cursor))
                return DateTime.UtcNow;

            var decodedBytes = Convert.FromBase64String(cursor);
            return DateTime.ParseExact(Encoding.UTF8.GetString(decodedBytes), "o", null);
        }

        private string GenerateCursor(DateTime lastPostTimestamp)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(lastPostTimestamp.ToString("o")));
        }
    }
}
