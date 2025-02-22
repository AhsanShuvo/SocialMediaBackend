using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Models.Requests;

namespace SocialMediaBackend.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
        }

        [HttpDelete("delete-user/{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
        {
            var result = await _userService.DeleteUserAndAllDataAsync(userId);
            return result ? NoContent() : NotFound();
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            var response = await _userService.CreateUserAsync(request);
            if (response)
            {
                return Ok(CreateResponse.Success("successfully created a new user."));
            }
            else
            {
                return StatusCode(500, CreateResponse.Failure("Failed to create a new user."));
            }
        }
    }
}
