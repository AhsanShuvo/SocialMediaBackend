namespace SocialMediaBackend.API.Models
{
    public class CreateResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public CreateResponse(bool isSuccess, string message, int? errorCode = null)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static CreateResponse Success(string message) => new CreateResponse(true, message);
        public static CreateResponse Failure(string message) => new CreateResponse(false, message);
    }
}
