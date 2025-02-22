using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly.Retry;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Helper;
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
        private readonly AsyncRetryPolicy _retryPolicy;

        public PostService(ILogger<PostService> logger, ICacheService cacheService, AppDbContext appDbContext, IOptions<AzureStorageSettings> options)
        {
            _logger = logger;
            _cacheService = cacheService;
            _context = appDbContext;
            _settings = options.Value;
            _retryPolicy = RetryPolicies.CreateRetryPolicy(_logger);
        }

        public async Task<PaginatedPostResponse> GetAllPostsAsync(int limit, string? cursor)
        {
            var lastPostTimeStamp = DecodeCursor(cursor);

            var cachedData = await _cacheService.GetPaginatedPostsAsync(new DateTimeOffset(lastPostTimeStamp).ToUnixTimeMilliseconds(), limit);
            List<PostDto> posts;

            if (cachedData.Any())
            {
                var cachedPostsTask = cachedData.Select(async postId =>
                {
                    var comments = await _cacheService.GetLatestCommentsAsync(postId);
                    var post = await _cacheService.GetPostByIdAsync(postId);
                    return new PostDto
                    {
                        Id = post.Id,
                        Caption = post.Caption,
                        ImageUrl = post.ImageUrl,
                        Creator = new CreatorDto
                        {
                            Id = post.CreatorId
                        },
                        RecentComments = comments
                                .OrderByDescending(comment => comment.CreatedAt)
                                .Take(2)
                                .Select(c => new CommentDto
                                {
                                    Id = c.Id,
                                    Content = c.Content,
                                    CreatorDto = new CreatorDto
                                    {
                                        Id = c.CreatorId,
                                    },
                                    CreatedAt = c.CreatedAt
                                })
                                .ToList(),
                        CreatedAt = post.CreatedAt
                    };
                });

                posts = (await Task.WhenAll(cachedPostsTask)).ToList();
            }
            else
            {
                _logger.LogInformation("Cache miss. Fetching data from database.");
                var totalposts = _context.Posts;

                posts = await _context.Posts
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
            }
            posts.ForEach(p =>
            {
                p.ImageUrl = p.ImageUrl.Replace(_settings.OriginalContainerName, _settings.ProcessedContainerName);
            });

            var nextCursor = posts.Count() > 0 ? GenerateCursor(posts.Last().CreatedAt) : GenerateCursor(lastPostTimeStamp);

            var paginatedPosts = new PaginatedPostResponse
            {
                Posts = posts,
                NextPageToken = nextCursor
            };

            return paginatedPosts;
        }

        public async Task<bool> CreatePostAsync(CreatePostRequest reqeust)
        {
            var timestamp = DateTime.UtcNow;
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Caption = reqeust.Caption,
                ImageUrl = reqeust.ImageUrl,
                CreatorId = reqeust.CreatorId,
                CreatedAt = timestamp
            };
            
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    _context.Posts.Add(post);
                    await _context.SaveChangesAsync();
                    await _cacheService.AddPostAsync(post.Id.ToString(), new DateTimeOffset(timestamp).ToUnixTimeMilliseconds(), post);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create a post after retries for CreatorId: {CreatorId}", reqeust.CreatorId);
                return false;
            }

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
