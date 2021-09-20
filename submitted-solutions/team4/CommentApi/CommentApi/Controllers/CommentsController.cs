using CommentApi.Extensions;
using CommentApi.Models;
using CommentApi.NaiveImplementation;
using Microsoft.AspNetCore.Mvc;

namespace CommentApi.Controllers
{
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly CommentService _commentService;
        private readonly Func<CommentService> _commentServiceFactory;

        public CommentsController(
            CommentService commentService,
            Func<CommentService> commentServiceFactory)
        {
            _commentService = commentService;
            _commentServiceFactory = commentServiceFactory;
        }

        [HttpPost("webPage/{location}/comment")]
        public async Task<ActionResult<CommentDto>> PostComment(
            string location,
            [FromBody] CommentInput comment)
        {
            var newComment = await _commentService.CreateNewCommentAsync(comment.Parent, comment.Author, comment.Text, location);

            if (newComment is null)
            {
                return BadRequest();
            }

            return Created($"comment/{newComment.Id}", newComment);
        }
    }
}
