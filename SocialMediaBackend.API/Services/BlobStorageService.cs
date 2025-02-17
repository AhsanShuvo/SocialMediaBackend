using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Settings;

namespace SocialMediaBackend.API.Services
{
    public class BlobStorageService : IImageStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly AzureStorageSettings _settings;

        private static readonly HashSet<string> AllowedExtensions = new() { ".png", ".jpg", ".bmp" };
        private const int MaxFileSize = 100 * 1024 * 1024;

        public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureStorageSettings> settings)
        {
            _settings = settings.Value;
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(_settings.OriginalContainerName);
        }

        public string GenerateUploadUrl(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Allowed: .png, .jpg, .bmp");

            var blobClient = _blobContainerClient.GetBlobClient(fileName);

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
            return sasToken.ToString();
        }
    }
}
