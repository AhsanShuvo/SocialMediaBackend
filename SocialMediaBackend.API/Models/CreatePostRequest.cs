namespace SocialMediaBackend.API.Models
{
    public class CreatePostRequest
    {
        public string Caption { get; set; }
        public string ImageUrl { get; set; }
        public Guid CreatorId { get; set; }
    }
}
