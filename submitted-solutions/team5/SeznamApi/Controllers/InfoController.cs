using Microsoft.AspNetCore.Mvc;
using SeznamApi.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SeznamApi.Controllers
{
    public class InfoController : ControllerBase
    {
        private readonly ICommentsOrchestration _commentsOrchestration;
        public InfoController(ICommentsOrchestration commentsOrchestration)
        {
            _commentsOrchestration = commentsOrchestration;
        }

        [Route("info")]
        public IActionResult Index()
        {
            return Ok("Tento sw má budoucnost!");
        }

        [Route("webPage/{location}/comment")]
        [HttpGet]
        public IActionResult GetComment([FromRoute] string location)
        {
            var comment = _commentsOrchestration.GetCommentByUrl(location);
            if (comment != null) { return Ok(comment); } else { return BadRequest(); };
        }

        [Route("webPage/{location}/comment")]
        [HttpPost]
        public IActionResult PostComment([FromRoute] string location)
        {

            return Ok(location);
        }

        [Route("comment/{id}")]
        [HttpGet]
        public IActionResult GetCommentById([FromRoute] int id)
        {
            return Ok(id);
        }

        [Route("comment")]
        [HttpDelete]
        public IActionResult DeleteAllComments()
        {
            return Ok();
        }

        [Route("batch")]
        [HttpPost]
        public IActionResult PostBatch([FromRoute] string? location)
        {
            return Ok(location);
        }
    }
}
