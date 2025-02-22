namespace SocialMediaBackend.API.Models.Requests
{
    public class UserCreateRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }
}
