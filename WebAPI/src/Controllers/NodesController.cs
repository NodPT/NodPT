using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data.DTOs;
using NodPT.Data.Services;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class NodesController : ControllerBase
    {
        private readonly NodeService _nodeService = new();

        [HttpGet]
        public IActionResult GetNodes() => Ok(_nodeService.GetAllNodes());

        [HttpGet("{id}")]
        public IActionResult GetNode(string id)
        {
            var node = _nodeService.GetNode(id);
            return node == null ? NotFound() : Ok(node);
        }

        [HttpPost]
        public IActionResult CreateNode([FromBody] NodeDto node)
        {
            if (node == null) return BadRequest();

            node.Id = Guid.NewGuid().ToString();
            node.CreatedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;

            _nodeService.AddNode(node);
            return CreatedAtAction(nameof(GetNode), new { id = node.Id }, node);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateNode(string id, [FromBody] NodeDto node)
        {
            if (node == null || node.Id != id) return BadRequest();

            var existingNode = _nodeService.GetNode(id);
            if (existingNode == null) return NotFound();

            node.UpdatedAt = DateTime.UtcNow;
            _nodeService.UpdateNode(node);
            return Ok(node);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteNode(string id)
        {
            var node = _nodeService.GetNode(id);
            if (node == null) return NotFound();

            _nodeService.DeleteNode(id);
            return NoContent();
        }
    }
}
