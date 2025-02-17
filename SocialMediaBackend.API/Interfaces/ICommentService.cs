using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface ICommentService
    {
        Task<bool> CreateCommentAsync(Comment comment);
    }
}
