using Microsoft.EntityFrameworkCore;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;

namespace SocialMediaBackend.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, JwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<string?> AuthenticateAsync(Guid userId)
        {
            try
            {
                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return null;

                return _jwtService.GenerateToken(user.Id, user.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, " Failed to Authenticate user for user id: {id}", userId);
                throw;
            }
        }
    }
}
