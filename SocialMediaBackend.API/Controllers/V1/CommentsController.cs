using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ILogger<CommentsController> _logger;
        private readonly ICommentService _commentService;

        public CommentsController(ILogger<CommentsController> logger, ICommentService commentService)
        {
            _logger = logger;
            _commentService = commentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] Comment comment)
        {
            await _commentService.CreateCommentAsync(comment);
            return Ok(new { message = "Comment added successfully." });
        }
    }
}
