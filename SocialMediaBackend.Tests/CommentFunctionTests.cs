using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.Functions;
using System.Text;

namespace SocialMediaBackend.Tests
{
    public class CommentFunctionTests
    {
        private readonly Mock<ILogger<CommentFunction>> _loggerMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ServiceBusMessageActions> _messageActionsMock;

        public CommentFunctionTests()
        {
            _loggerMock = new Mock<ILogger<CommentFunction>>();
            _cacheServiceMock = new Mock<ICacheService>();
            _messageActionsMock = new Mock<ServiceBusMessageActions>();   
        }

        [Fact]
        public async Task RunAsync_ShouldProcessValidCommentMessage()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _commentFunction = new CommentFunction(
                _loggerMock.Object,
                _dbContext,
                _cacheServiceMock.Object
            );

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = "This is a test comment",
                CreatorId = Guid.NewGuid(),
                PostId = Guid.NewGuid()
            };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comment));
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes(messageBody));

            _cacheServiceMock.Setup(c => c.RemoveAsync("posts:")).Returns(Task.CompletedTask);
            _messageActionsMock.Setup(m => m.CompleteMessageAsync(message, default)).Returns(Task.CompletedTask);

            // Act
            await _commentFunction.RunAsync(message, _messageActionsMock.Object);

            // Assert
            var commentInDb = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == comment.Id);
            Assert.NotNull(commentInDb);
            Assert.Equal(comment.Content, commentInDb.Content);

            _cacheServiceMock.Verify(c => c.RemoveAsync("posts:"), Times.Once);
            _messageActionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldDeadLetterMessage_WhenMessageIsInvalid()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _commentFunction = new CommentFunction(
                _loggerMock.Object,
                _dbContext,
                _cacheServiceMock.Object
            );

            var invalidMessageBody = Encoding.UTF8.GetBytes(""); // Empty message
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes(invalidMessageBody));

            _messageActionsMock.Setup(m => m.DeadLetterMessageAsync(message, default, default, default, default)).Returns(Task.CompletedTask);

            // Act
            await _commentFunction.RunAsync(message, _messageActionsMock.Object);

            // Assert
            _messageActionsMock.Verify(m => m.DeadLetterMessageAsync(message, default, default, default, default), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldAbandonMessage_WhenExceptionOccurs()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: "TestDb").Options;
            var _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            var _commentFunction = new CommentFunction(
                _loggerMock.Object,
                _dbContext,
                _cacheServiceMock.Object
            );

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = "This comment will fail",
                CreatorId = Guid.NewGuid(),
                PostId = Guid.NewGuid()
            };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comment));
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes(messageBody));

            // Mock a failing DbContext
            var dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                         .ThrowsAsync(new Exception("Database error"));

            var failingCommentFunction = new CommentFunction(_loggerMock.Object, dbContextMock.Object, _cacheServiceMock.Object);

            _messageActionsMock.Setup(m => m.AbandonMessageAsync(message, default, default))
                               .Returns(Task.CompletedTask);

            // Act 
            await failingCommentFunction.RunAsync(message, _messageActionsMock.Object);

            // Assert
            _messageActionsMock.Verify(m => m.AbandonMessageAsync(message, default, default), Times.Once);
        }
    }
}
