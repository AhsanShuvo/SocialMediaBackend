using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models.Requests;
using SocialMediaBackend.API.Services;

namespace SocialMediaBackend.API.Controllers.V1
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.AuthenticateAsync(request.UserId);
            if (token == null)
            {
                _logger.LogWarning("Authentication failed. Invalid user Id: {userId}.", request.UserId);
                return Unauthorized(new { message = "Invalid user Id." });
            }
            _logger.LogInformation("Authentication successful for user: {name}", request.Name);

            return Ok(new { token });
        }
    }
}
