using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Settings;

namespace SocialMediaBackend.API.Services
{
    public class BlobStorageService : IImageStorageService
    {
        private readonly BlobContainerClient _blobOriginalContainerClient;
        private readonly BlobContainerClient _blobProcessedContainerClient;
        private readonly AzureStorageSettings _settings;
        private readonly ILogger<BlobStorageService> _logger;

        private static readonly HashSet<string> AllowedExtensions = new() { ".png", ".jpg", ".bmp" };
        private const int MaxFileSize = 100 * 1024 * 1024;

        public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureStorageSettings> settings, ILogger<BlobStorageService> logger)
        {
            _settings = settings.Value;
            _blobOriginalContainerClient = blobServiceClient.GetBlobContainerClient(_settings.OriginalContainerName);
            _blobProcessedContainerClient = blobServiceClient.GetBlobContainerClient(_settings.ProcessedContainerName);
            _logger = logger;
        }

        public BlobUploadUrlResponse GenerateUploadUrl(string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLower();
                if (!AllowedExtensions.Contains(extension))
                {
                    return BlobUploadUrlResponse.Failure("Invalid file type. Allowed: .png, .jpg, .bmp");
                }

                var blobClient = _blobOriginalContainerClient.GetBlobClient(fileName);

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _settings.OriginalContainerName,
                    BlobName = fileName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_settings.UrlExpiresInMinute)
                };

                sasBuilder.SetPermissions(BlobContainerSasPermissions.Write);

                var sasToken = blobClient.GenerateSasUri(sasBuilder);
                return BlobUploadUrlResponse.Success(sasToken.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to generate upload url for filename: {filename}", fileName);
                return BlobUploadUrlResponse.Failure(ex.Message);
            }
        }

        public async Task DeleteBlobImagesAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;

            Uri uri = new Uri(imageUrl);
            string blobName = uri.Segments[^1];

            var processedBlobLClient = _blobProcessedContainerClient.GetBlobClient(blobName);
            var originalBlobClient = _blobOriginalContainerClient.GetBlobClient(blobName);

            await Task.WhenAll(processedBlobLClient.DeleteIfExistsAsync(), originalBlobClient.DeleteIfExistsAsync());
        }
    }
}
