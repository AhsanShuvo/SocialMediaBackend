using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IImageStorageService
    {
        BlobUploadUrlResponse GenerateUploadUrl(string fileName);
        Task DeleteBlobImagesAsync(string imageUrl);
    }
}
