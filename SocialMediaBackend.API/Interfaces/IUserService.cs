using SocialMediaBackend.API.Models.Requests;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IUserService
    {
        Task<bool> DeleteUserAndAllDataAsync(Guid userId);
        Task<bool> CreateUserAsync(UserCreateRequest request);
    }
}
