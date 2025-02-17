using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IPostService
    {
        Task<IEnumerable<Post>> GetAllPostsAsync(int limit, string? cursor);
        Task<Post> CreatePostAsync(CreatePostRequest request);
    }
}
