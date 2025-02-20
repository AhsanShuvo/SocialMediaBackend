namespace SocialMediaBackend.API.Models
{
    public class PaginatedPostResponse
    {
        public List<PostDto> Posts { get; set; }
        public string? NextPageToken { get; set; }
    }

    public class PostDto
    {
        public Guid Id { get; set; }
        public string Caption { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public CreatorDto Creator { get; set; }
        public List<CommentDto> RecentComments { get; set; } = new List<CommentDto>();
    }

    public class CreatorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class CommentDto
    {
        public Guid Id { get; set; }
        public CreatorDto CreatorDto { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
