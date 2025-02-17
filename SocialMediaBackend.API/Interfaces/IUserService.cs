namespace SocialMediaBackend.API.Interfaces
{
    public interface IUserService
    {
        Task<bool> DeleteUserAndAllDataAsync(Guid userId);
    }
}
