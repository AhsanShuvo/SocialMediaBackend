using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models.Requests;
using SocialMediaBackend.API.Services;
using SocialMediaBackend.API.Settings;

namespace SocialMediaBackend.Tests
{
    public class CommentServiceTests
    {
        private readonly Mock<ServiceBusClient> _serviceBusClientMock;
        private readonly Mock<ServiceBusSender> _serviceBusSenderMock;
        private readonly Mock<IOptions<ServiceBusSettings>> _optionsMock;
        private readonly CommentService _commentService;
        private readonly Mock<ILogger<CommentService>> _loggerMock;
        private readonly Mock<ICacheService> _cacheServiceMock;

        public CommentServiceTests()
        {
            _serviceBusClientMock = new Mock<ServiceBusClient>();
            _serviceBusSenderMock = new Mock<ServiceBusSender>();
            _optionsMock = new Mock<IOptions<ServiceBusSettings>>();

            var serviceBusSettings = new ServiceBusSettings { QueueName = "test-queue" };
            _optionsMock.Setup(o => o.Value).Returns(serviceBusSettings);
            _serviceBusClientMock.Setup(c => c.CreateSender(serviceBusSettings.QueueName))
                                 .Returns(_serviceBusSenderMock.Object);
            _loggerMock = new Mock<ILogger<CommentService>>();
            _cacheServiceMock = new Mock<ICacheService>();

            _commentService = new CommentService(_serviceBusClientMock.Object, _optionsMock.Object, _loggerMock.Object, _cacheServiceMock.Object);
        }

        [Fact]
        public async Task CreateCommentAsync_ShouldSendMessage_WhenRequestIsValid()
        {
            // Arrange
            var request = new CommentRequest
            {
                Content = "Test comment",
                CreatorId = Guid.NewGuid(),
                PostId = Guid.NewGuid()
            };
            _serviceBusSenderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                                 .Returns(Task.CompletedTask)
                                 .Verifiable();

            // Act
            var result = await _commentService.CreateCommentAsync(request);

            // Assert
            Assert.True(result);
            _serviceBusSenderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }
    }
}