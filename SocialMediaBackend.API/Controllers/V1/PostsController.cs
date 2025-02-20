using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Services;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

namespace SocialMediaBackend.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private readonly IImageStorageService _blobStorageService;
        private readonly IPostService _postService;

        public PostsController(ILogger<PostsController> logger, IImageStorageService blobStorageService, IPostService postService)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _postService = postService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PaginatedPostResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> GetAllPosts([FromQuery] int limit = 10, [FromQuery] string? cursor = null)
        {
           var stopwatch = Stopwatch.StartNew();
           var posts = await _postService.GetAllPostsAsync(limit, cursor);
            stopwatch.Stop();
            _logger.LogInformation("Elapsed time: {TotalMilliseconds} ms", stopwatch.Elapsed.TotalMilliseconds);
            return Ok(posts);
        }

        [HttpPost]
        [Route("create")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePostAsync(CreatePostRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "");
            
            if(userId != request.CreatorId)
            {
                return Unauthorized("Unauthorized Creator Id");
            }

            await _postService.CreatePostAsync(request);
            return Ok(new { message = "post was created successfully."});
        }

        [HttpGet("upload-url")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetUploadUrl([FromQuery] string fileName)
        {
            var uploadUrlResponse = _blobStorageService.GenerateUploadUrl(fileName);

            return Ok(new { uploadUrlResponse });
        }
    }
}
