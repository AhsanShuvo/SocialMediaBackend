namespace SocialMediaBackend.API.Models.Requests
{
    public class LoginRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }
}
