using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Services;

namespace SocialMediaBackend.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private readonly BlobStorageService _blobStorageService;
        private readonly IPostService _postService;

        public PostsController(ILogger<PostsController> logger, BlobStorageService blobStorageService, IPostService postService)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _postService = postService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPosts([FromQuery] int limit = 10, [FromQuery] string? cursor = null)
        {
           var posts = await _postService.GetAllPostsAsync(limit, cursor);
           return Ok(posts);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePostAsync(CreatePostRequest request)
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? "");
            
            if(userId != request.CreatorId)
            {
                return Unauthorized("Unauthorized Creator Id");
            }

            var post = await _postService.CreatePostAsync(request);
            return Created("", post);
        }

        [HttpGet("upload-url")]
        [Authorize]
        public IActionResult GetUploadUrl([FromQuery] string fileName)
        {
            var uploadUrl = _blobStorageService.GenerateUploadUrl(fileName);
            return Ok(new { uploadUrl });
        }
    }
}
