using Microsoft.EntityFrameworkCore;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;

namespace SocialMediaBackend.API.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IImageStorageService _imageStorageService;

        public UserService(ILogger<UserService> logger, AppDbContext context, ICacheService cacheService, IImageStorageService imageStorageService)
        {
            _logger = logger;
            _context = context;
            _cacheService = cacheService;
            _imageStorageService = imageStorageService;
        }

        public async Task<bool> DeleteUserAndAllDataAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Deleting user with all assciociated posts and comments for userId: {userId}", userId);

                var images = await _context.Posts
                    .AsNoTracking()
                    .Where(p => p.CreatorId == userId)
                    .Select(p => p.ImageUrl)
                    .ToListAsync();

                var user = await _context.Accounts
                    .Include(a => a.Posts).Include(a => a.Comments).FirstOrDefaultAsync(a => a.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User does not exist with userId: {userId}", userId);
                    return false;
                }

                _context.Accounts.Remove(user);
                await _context.SaveChangesAsync();

                await Task.WhenAll(images.Select(image => _imageStorageService.DeleteBlobImagesAsync(image)));

                await _cacheService.RemoveAsync("posts:");

                _logger.LogInformation("Successfully deleted user with userId: {userId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to delete user with userId: {userId}", userId);
                throw;
            }
        }
    }
}
