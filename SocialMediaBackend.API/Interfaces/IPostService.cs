using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IPostService
    {
        Task<PaginatedPostResponse> GetAllPostsAsync(int limit, string? cursor);
        Task<bool> CreatePostAsync(CreatePostRequest request);
    }
}
