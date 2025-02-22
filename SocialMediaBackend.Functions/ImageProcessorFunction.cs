using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SocialMediaBackend.API.Settings;

namespace SocialMediaBackend.Functions
{
    public class ImageProcessorFunction
    {
        private readonly ILogger<ImageProcessorFunction> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureStorageSettings _settings;

        public ImageProcessorFunction(ILogger<ImageProcessorFunction> logger, BlobServiceClient blobServiceClient, IOptions<AzureStorageSettings> options)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            _settings = options.Value;
        }

        [Function(nameof(ImageProcessorFunction))]
        public async Task RunAsync([BlobTrigger("%AzureStorage:OriginalContainerName%/{name}", Connection = "AzureStorage:ConnectionString")] Stream imageStream, string name)
        {
            try
            {
                _logger.LogInformation("Processing image with name: {name}", name);

                var processedContainerClient = _blobServiceClient.GetBlobContainerClient(_settings.ProcessedContainerName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(name);

                var processedBlobClient = processedContainerClient.GetBlobClient(fileNameWithoutExtension + ".jpg");

                using var inputStream = await processedBlobClient.OpenReadAsync();
                await using var outputStream = new MemoryStream();

                using var image = await Image.LoadAsync(imageStream);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(600, 600),
                    Mode = ResizeMode.Stretch
                }));

                await image.SaveAsync(outputStream, new JpegEncoder());
                outputStream.Position = 0;
                await processedBlobClient.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = "image/jpeg" });

                _logger.LogInformation($"Image {name} processed and saved as {name}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing image {name}: {ex.Message}");
                throw;
            }
        }
    }
}
