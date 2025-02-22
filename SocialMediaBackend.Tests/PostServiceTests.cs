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

            var cursor = "";
            var limit = 2;
            var postId = Guid.NewGuid().ToString();
            var cachedPost = new Post { Id = Guid.Parse(postId), Caption = "Test Caption", CreatedAt = DateTime.UtcNow, ImageUrl = "www.example.com/test-images/image.com" };

            _cacheServiceMock.Setup(cs => cs.GetPaginatedPostsAsync(It.IsAny<long>(), limit))
                .ReturnsAsync(new List<string> { postId });
            _cacheServiceMock.Setup(cs => cs.GetPostByIdAsync(postId))
                .ReturnsAsync(cachedPost);
            _cacheServiceMock.Setup(cs => cs.GetLatestCommentsAsync(postId))
                .ReturnsAsync(new List<Comment>());



            // Act
            var result = await _postService.GetAllPostsAsync(limit, cursor);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Posts);
            Assert.Equal("Test Caption", result.Posts[0].Caption);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnDatabasePosts_WhenCacheIsEmpty()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb2").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _postService = new PostService(_loggerMock.Object, _cacheServiceMock.Object, _dbContext, _optionsMock.Object);

            var cursor = "";
            var limit = 2;
            var creator = new Account { Id = Guid.NewGuid(), Name = "ahsan" };
            _dbContext.Accounts.Add(creator);
            _dbContext.SaveChanges();
            
            var post = new Post { Id = Guid.NewGuid(), Caption = "DB Post", CreatedAt = DateTime.UtcNow.AddHours(-2), ImageUrl = "www.example.com/test-images/image.com", CreatorId = creator.Id };
            await _dbContext.Posts.AddAsync(post);
            await _dbContext.SaveChangesAsync();

            var posts = _dbContext.Posts;

            _cacheServiceMock.Setup(cs => cs.GetPaginatedPostsAsync(It.IsAny<long>(), limit))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _postService.GetAllPostsAsync(limit, cursor);

            // Assert
            Assert.NotNull(result);
            Assert.Single(_dbContext.Posts);
            Assert.Single(result.Posts);
            Assert.Equal("DB Post", result.Posts[0].Caption);
        }

        [Fact]
        public async Task CreatePostAsync_ShouldAddPost_AndAddToCache()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb3").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _postService = new PostService(_loggerMock.Object, _cacheServiceMock.Object, _dbContext, _optionsMock.Object);

            var request = new CreatePostRequest { Caption = "New Post", ImageUrl = "www.example.com/test-images/image.com", CreatorId = Guid.NewGuid() };

            _cacheServiceMock.Setup(cs => cs.AddPostAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<Post>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _postService.CreatePostAsync(request);

            // Assert
            Assert.True(result);
            Assert.Single(_dbContext.Posts);
            Assert.Equal("New Post", _dbContext.Posts.First().Caption);
        }

    }

}
