namespace SocialMediaBackend.API.Interfaces
{
    public interface IImageStorageService
    {
        string GenerateUploadUrl(string fileName);
    }
}
