using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SocialMediaBackend.API.Services;
using SocialMediaBackend.API.Settings;

namespace SocialMediaBackend.Tests
{
    public class BlobStorageServiceTests
    {
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly Mock<BlobContainerClient> _originalContainerMock;
        private readonly Mock<BlobContainerClient> _processedContainerMock;
        private readonly Mock<BlobClient> _blobClientMock;
        private readonly Mock<IOptions<AzureStorageSettings>> _settingsMock;
        private readonly Mock<ILogger<BlobStorageService>> _loggerMock;
        private readonly BlobStorageService _blobStorageService;

        public BlobStorageServiceTests()
        {
            _blobServiceClientMock = new Mock<BlobServiceClient>();
            _originalContainerMock = new Mock<BlobContainerClient>();
            _processedContainerMock = new Mock<BlobContainerClient>();
            _blobClientMock = new Mock<BlobClient>();
            _settingsMock = new Mock<IOptions<AzureStorageSettings>>();
            _loggerMock = new Mock<ILogger<BlobStorageService>>();

            var storageSettings = new AzureStorageSettings
            {
                OriginalContainerName = "original-container",
                ProcessedContainerName = "processed-container",
                UrlExpiresInMinute = 10
            };
            _settingsMock.Setup(s => s.Value).Returns(storageSettings);

            // Mock BlobServiceClient to return container clients
            _blobServiceClientMock.Setup(s => s.GetBlobContainerClient(storageSettings.OriginalContainerName))
                .Returns(_originalContainerMock.Object);

            _blobServiceClientMock.Setup(s => s.GetBlobContainerClient(storageSettings.ProcessedContainerName))
                .Returns(_processedContainerMock.Object);

            // Instantiate BlobStorageService with mocks
            _blobStorageService = new BlobStorageService(
                _blobServiceClientMock.Object,
                _settingsMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public void GenerateUploadUrl_ShouldReturnSuccess_WhenFileTypeIsValid()
        {
            // Arrange
            var fileName = "test.jpg";
            _originalContainerMock.Setup(c => c.GetBlobClient(fileName)).Returns(_blobClientMock.Object);

            var expectedUrl = new Uri("https://example.com/sas-token");
            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>())).Returns(expectedUrl);

            // Act
            var result = _blobStorageService.GenerateUploadUrl(fileName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedUrl.ToString(), result.Url);
        }

        [Fact]
        public void GenerateUploadUrl_ShouldReturnFailure_WhenFileTypeIsInvalid()
        {
            // Arrange
            var fileName = "test.txt"; // Invalid file type

            // Act
            var result = _blobStorageService.GenerateUploadUrl(fileName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid file type. Allowed: .png, .jpg, .bmp", result.ErrorMessage);
        }

        [Fact]
        public void GenerateUploadUrl_ShouldHandleExceptions()
        {
            // Arrange
            var fileName = "test.jpg";
            _originalContainerMock.Setup(c => c.GetBlobClient(fileName)).Throws(new Exception("Storage error"));

            // Act
            var result = _blobStorageService.GenerateUploadUrl(fileName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Storage error", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteBlobImagesAsync_ShouldDeleteBlobs_WhenUrlIsValid()
        {
            // Arrange
            var imageUrl = "https://storageaccount.blob.core.windows.net/test-images/test.jpg";
            var blobName = "test.jpg";

            var originalBlobMock = new Mock<BlobClient>();
            var processedBlobMock = new Mock<BlobClient>();

            _originalContainerMock.Setup(c => c.GetBlobClient(blobName)).Returns(originalBlobMock.Object);
            _processedContainerMock.Setup(c => c.GetBlobClient(blobName)).Returns(processedBlobMock.Object);

            originalBlobMock.Setup(b => b.DeleteIfExistsAsync(default, null, default)).ReturnsAsync(Response.FromValue(true, null));
            processedBlobMock.Setup(b => b.DeleteIfExistsAsync(default, null, default)).ReturnsAsync(Response.FromValue(true, null));

            // Act
            await _blobStorageService.DeleteBlobImagesAsync(imageUrl);

            // Assert
            originalBlobMock.Verify(b => b.DeleteIfExistsAsync(default, null, default), Times.Once);
            processedBlobMock.Verify(b => b.DeleteIfExistsAsync(default, null, default), Times.Once);
        }

        [Fact]
        public async Task DeleteBlobImagesAsync_ShouldDoNothing_WhenUrlIsEmpty()
        {
            // Act
            await _blobStorageService.DeleteBlobImagesAsync("");

            // Assert
            _originalContainerMock.Verify(c => c.GetBlobClient(It.IsAny<string>()), Times.Never);
            _processedContainerMock.Verify(c => c.GetBlobClient(It.IsAny<string>()), Times.Never);
        }
    }
}
