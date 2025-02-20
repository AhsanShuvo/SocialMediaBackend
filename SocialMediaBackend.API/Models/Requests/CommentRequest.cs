namespace SocialMediaBackend.API.Models.Requests
{
    public class CommentRequest
    {
        public string Content { get; set; }
        public Guid CreatorId { get; set; }
        public Guid PostId { get; set; }
    }
}
