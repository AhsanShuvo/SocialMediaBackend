using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Models.Requests;

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
        [ProducesResponseType(typeof(CommentRequest), StatusCodes.Status201Created)]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateComment([FromBody] CommentRequest comment)
        {
            var result = await _commentService.CreateCommentAsync(comment);

            var response = new
            {
                Success = result,
                Message = result == true ? "Comment added successfully" : "Failed to add comment"
            };

            return Ok(response);
        }
    }
}
