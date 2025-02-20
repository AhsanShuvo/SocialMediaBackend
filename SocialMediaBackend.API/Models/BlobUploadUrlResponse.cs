namespace SocialMediaBackend.API.Models
{
    public class BlobUploadUrlResponse
    {
        public bool IsSuccess { get; set; }
        public string? Url { get; set; }
        public string? ErrorMessage { get; set; }

        public BlobUploadUrlResponse(bool isSuccess, string? url = null, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            Url = url;
            ErrorMessage = errorMessage;
        }

        public static BlobUploadUrlResponse Success(string url) => new BlobUploadUrlResponse(true, url);
        public static BlobUploadUrlResponse Failure(string errorMessage) => new BlobUploadUrlResponse(false, null, errorMessage);
    }
}
