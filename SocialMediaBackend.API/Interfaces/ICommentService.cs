using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Models.Requests;

namespace SocialMediaBackend.API.Interfaces
{
    public interface ICommentService
    {
        Task<bool> CreateCommentAsync(CommentRequest comment);
    }
}
