using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SocialMediaBackend.API.BackgroundServices
{
    public class ImageProcessingService : BackgroundService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _originalContainer;
        private readonly string _processedContainer;

        public ImageProcessingService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
            _originalContainer = configuration["AzureStorage:OriginalContainerName"];
            _processedContainer = configuration["AzureStorage:ProcessedContainerName"];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_originalContainer);
            await foreach (var blob in blobContainerClient.GetBlobsAsync())
            {
                if (stoppingToken.IsCancellationRequested) break;

                if (await IsAlreadyProcessed(blob.Name)) continue;

                var inputBlobClient = blobContainerClient.GetBlobClient(blob.Name);
                var outputBlobClient = _blobServiceClient.GetBlobContainerClient(_processedContainer).GetBlobClient(blob.Name + ".jpg");

                using var inputStream = await inputBlobClient.OpenReadAsync();
                using var image = await Image.LoadAsync(inputStream);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(600, 600),
                    Mode = ResizeMode.Max
                }));

                await using var outputStream = new MemoryStream();
                await image.SaveAsync(outputStream, new JpegEncoder());
                outputStream.Position = 0;

                await outputBlobClient.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = "image/jpeg" });
            }
        }

        private async Task<bool> IsAlreadyProcessed(string fileName)
        {
            var processedContainer = _blobServiceClient.GetBlobContainerClient(_processedContainer);
            return await processedContainer.GetBlobClient(fileName + ".jpg").ExistsAsync();
        }
    }
}
