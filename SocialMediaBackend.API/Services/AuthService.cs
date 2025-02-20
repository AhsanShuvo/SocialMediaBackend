using Microsoft.EntityFrameworkCore;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<TokenResponse> AuthenticateAsync(Guid userId)
        {
            try
            {
                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return TokenResponse.Failure(StatusCodes.Status404NotFound, "User Id does not exist.");

                var tokenData =  _jwtService.GenerateToken(user.Id, user.Name);

                return TokenResponse.Success(tokenData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, " Failed to get token for user id: {id}", userId);
                return TokenResponse.Failure(503, ex.Message);
            }
        }
    }
}
