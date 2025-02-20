using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Services;

namespace SocialMediaBackend.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IImageStorageService> _imageStorageServiceMock;

        public UserServiceTests()
        {
            _loggerMock = new Mock<ILogger<UserService>>();
            _cacheServiceMock = new Mock<ICacheService>();
            _imageStorageServiceMock = new Mock<IImageStorageService>();
        }

        [Fact]
        public async Task DeleteUserAndAllDataAsync_ShouldDeleteUser_WhenUserExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _userService = new UserService(_loggerMock.Object, _dbContext, _cacheServiceMock.Object, _imageStorageServiceMock.Object);

            var userId = Guid.NewGuid();

            var user = new Account
            {
                Id = userId,
                Name = "Test User",
                Posts = new List<Post>
            {
                new Post { Id = Guid.NewGuid(), CreatorId = userId, Caption = "Post 1", ImageUrl = "https://example.com/test-images/image1.jpg" },
                new Post { Id = Guid.NewGuid(), CreatorId = userId, Caption = "Post 2", ImageUrl = "https://example.com/test-images/image2.jpg" }
            },
                Comments = new List<Comment>
            {
                new Comment { Id = Guid.NewGuid(), CreatorId = userId, Content = "Test Comment 1" },
                new Comment { Id = Guid.NewGuid(), CreatorId = userId, Content = "Test Comment 2" }
            }
            };

            await _dbContext.Accounts.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            _imageStorageServiceMock.Setup(s => s.DeleteBlobImagesAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _cacheServiceMock.Setup(c => c.RemoveAsync("posts:"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.DeleteUserAndAllDataAsync(userId);

            // Assert
            Assert.True(result);

            var userInDb = await _dbContext.Accounts.FindAsync(userId);
            Assert.Null(userInDb);

            _imageStorageServiceMock.Verify(s => s.DeleteBlobImagesAsync("https://example.com/test-images/image1.jpg"), Times.Once);
            _imageStorageServiceMock.Verify(s => s.DeleteBlobImagesAsync("https://example.com/test-images/image2.jpg"), Times.Once);
            _cacheServiceMock.Verify(c => c.RemoveAsync("posts:"), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAndAllDataAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _userService = new UserService(_loggerMock.Object, _dbContext, _cacheServiceMock.Object, _imageStorageServiceMock.Object);

            var nonExistentUserId = Guid.NewGuid();

            // Act
            var result = await _userService.DeleteUserAndAllDataAsync(nonExistentUserId);

            // Assert
            Assert.False(result);
            _imageStorageServiceMock.Verify(s => s.DeleteBlobImagesAsync(It.IsAny<string>()), Times.Never);
            _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
