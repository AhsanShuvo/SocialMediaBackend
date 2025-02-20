using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IJwtService
    {
        TokenData GenerateToken(Guid userId, string name);
    }
}
