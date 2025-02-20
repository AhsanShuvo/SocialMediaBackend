using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Services;
using SocialMediaBackend.API.Settings;


namespace SocialMediaBackend.Tests
{
    public class PostServiceTests
    {
        private readonly Mock<ILogger<PostService>> _loggerMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IOptions<AzureStorageSettings>> _optionsMock;

        public PostServiceTests()
        {
            _loggerMock = new Mock<ILogger<PostService>>();
            _cacheServiceMock = new Mock<ICacheService>();
            _optionsMock = new Mock<IOptions<AzureStorageSettings>>();

            var storageSettings = new AzureStorageSettings
            {
                OriginalContainerName = "test-images",
                ProcessedContainerName = "images"
            };
            _optionsMock.Setup(o => o.Value).Returns(storageSettings);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnCachedData_WhenCacheIsAvailable()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _postService = new PostService(_loggerMock.Object, _cacheServiceMock.Object, _dbContext, _optionsMock.Object);

            string cursor = null;
            int limit = 5;
            var cacheKey = $"posts:cursor:{cursor}:limit:{limit}";

            var cachedResponse = new PaginatedPostResponse
            {
                Posts = new List<PostDto>
                {
                    new PostDto
                    {
                        Id = Guid.NewGuid(),
                        Caption = "Cached Post",
                        CreatedAt = DateTime.UtcNow,
                        ImageUrl = "https://example.com/original/test.jpg",
                        Creator = new CreatorDto { Id = Guid.NewGuid(), Name = "User1" }
                    }
                },
                NextPageToken = "nextCursor"
            };

            _cacheServiceMock.Setup(c => c.GetAsync<PaginatedPostResponse>(cacheKey))
                             .ReturnsAsync(cachedResponse);

            // Act
            var result = await _postService.GetAllPostsAsync(limit, cursor);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedResponse.Posts.Count, result.Posts.Count);
            _cacheServiceMock.Verify(c => c.GetAsync<PaginatedPostResponse>(cacheKey), Times.Once);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnDatabasePosts_WhenCacheIsEmpty()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb2").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _postService = new PostService(_loggerMock.Object, _cacheServiceMock.Object, _dbContext, _optionsMock.Object);

            string cursor = null;
            int limit = 2;
            var cacheKey = $"posts:cursor:{cursor}:limit:{limit}";

            var creator = new Account { Id = Guid.NewGuid(), Name = "Test Creator" };
            await _dbContext.Accounts.AddAsync(creator);

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Caption = "Database Post",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ImageUrl = "https://example.com/test-images/db_post.jpg",
                CreatorId = creator.Id,
                Creator = creator,
                Comments = new List<Comment>()
            };

            await _dbContext.Posts.AddAsync(post);
            await _dbContext.SaveChangesAsync();

            _cacheServiceMock.Setup(c => c.GetAsync<PaginatedPostResponse>(cacheKey))
                             .ReturnsAsync((PaginatedPostResponse)default);

            _cacheServiceMock.Setup(c => c.SetAsync(cacheKey, It.IsAny<PaginatedPostResponse>(), It.IsAny<TimeSpan>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _postService.GetAllPostsAsync(limit, cursor);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Posts);
            Assert.Equal(post.Caption, result.Posts[0].Caption);
            Assert.Contains("/images/", result.Posts[0].ImageUrl);
            _cacheServiceMock.Verify(c => c.SetAsync(cacheKey, It.IsAny<PaginatedPostResponse>(), null), Times.Once);
        }

        [Fact]
        public async Task CreatePostAsync_ShouldAddPost_AndClearCache()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb3").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _postService = new PostService(_loggerMock.Object, _cacheServiceMock.Object, _dbContext, _optionsMock.Object);

            var request = new CreatePostRequest
            {
                Caption = "New Post",
                ImageUrl = "https://example.com/test-images/new_post.jpg",
                CreatorId = Guid.NewGuid()
            };

            _cacheServiceMock.Setup(c => c.RemoveAsync("posts:*")).Returns(Task.CompletedTask);

            // Act
            var result = await _postService.CreatePostAsync(request);

            // Assert
            Assert.True(result);
            var postInDb = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Caption == request.Caption);
            Assert.NotNull(postInDb);
            Assert.Equal(request.Caption, postInDb.Caption);
            _cacheServiceMock.Verify(c => c.RemoveAsync("posts:*"), Times.Once);
        }

    }

}
