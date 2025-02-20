using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Services;

namespace SocialMediaBackend.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;

        public AuthServiceTests()
        {
            _jwtServiceMock = new Mock<IJwtService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
        }


        private TokenData _tokenData = new TokenData("user-token",3600);

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnSuccess_WhenUserExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();

            var userId = Guid.NewGuid();
            var user = new Account { Id = userId, Name = "Test User" };
            await _dbContext.Accounts.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            _jwtServiceMock.Setup(s => s.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(_tokenData);

            var authService = new AuthService(_dbContext, _jwtServiceMock.Object, _loggerMock.Object);

            // Act
            var result = await authService.AuthenticateAsync(userId);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(_tokenData, result.Data);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnFailure_WhenUserDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();

            var userId = Guid.NewGuid();
            var authService = new AuthService(_dbContext, _jwtServiceMock.Object, _loggerMock.Object);

            // Act
            var result = await authService.AuthenticateAsync(userId);

            // Assert
            Assert.Equal(404, result.StatusCode);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Equal("User Id does not exist.", result.Message);
        }
    }
}
