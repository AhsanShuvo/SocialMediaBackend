namespace SocialMediaBackend.API.Interfaces
{
    public interface IAuthService
    {
        Task<string?> AuthenticateAsync(Guid userId);
    }
}
