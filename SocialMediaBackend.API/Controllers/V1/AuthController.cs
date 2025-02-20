using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Models.Requests;

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

        [HttpPost("token")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetToken([FromBody] TokenRequest request)
        {
            var tokenResponse = await _authService.AuthenticateAsync(request.UserId);
            return Ok(tokenResponse);
        }
    }
}
