namespace SocialMediaBackend.API.Models.Requests
{
    public class TokenRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }
}
