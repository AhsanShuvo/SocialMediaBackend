using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface ICacheService
    {
        Task AddPostAsync(string postId, long timestamp, Post postContent);
        Task<List<string>> GetPaginatedPostsAsync(long? cursor, int limit);
        Task<Post?> GetPostByIdAsync(string postId);
        Task AddCommentToPostAsync(string postId, string commentId, Comment comment);
        Task<List<Comment>> GetLatestCommentsAsync(string postId);
        Task DeletePostAsync(string postId);
        Task DeleteCommentAsync(string postId, string commentId);
    }
}
