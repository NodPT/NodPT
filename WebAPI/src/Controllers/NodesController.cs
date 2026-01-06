using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data.DTOs;
using NodPT.Data.Services;
using NodPT.Data.Models;
using DevExpress.Xpo;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class NodesController : ControllerBase
    {
        private readonly UnitOfWork unitOfWork;
        private readonly NodeService _nodeService;

        public NodesController(UnitOfWork _unitOfWork)
        {
            this.unitOfWork = _unitOfWork;
            this._nodeService = new NodeService(unitOfWork);
        }

        [HttpGet]
        public IActionResult GetNodes()
        {
            try
            {
                return Ok(_nodeService.GetAllNodes());
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "GetNodes");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetNode(string id)
        {
            try
            {
                var node = _nodeService.GetNode(id);
                return node == null ? NotFound() : Ok(node);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "GetNode");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }

        [HttpGet("project/{projectId}")]
        public IActionResult GetNodesByProject(int projectId)
        {
            try
            {
                var nodes = _nodeService.GetNodesByProject(projectId);
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "GetNodesByProject");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }

        [HttpPost]
        public IActionResult CreateNode([FromBody] NodeDto node)
        {
            try
            {
                if (node == null) return BadRequest();

                // Get authenticated user
                var user = UserService.GetUser(User, unitOfWork);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not authorized" });
                }

                // Validate hierarchy if parent is provided
                if (!string.IsNullOrEmpty(node.ParentId))
                {
                    var parentNode = unitOfWork.Query<Node>().FirstOrDefault(n => n.Id == node.ParentId);
                    if (parentNode == null)
                    {
                        return BadRequest(new { error = "Parent node not found" });
                    }

                    // Verify parent belongs to user's project
                    if (parentNode.Project?.User?.Oid != user.Oid)
                    {
                        return Unauthorized(new { error = "Parent node does not belong to your project" });
                    }

                    // Validate hierarchy
                    if (!_nodeService.ValidateNodeHierarchy(parentNode.NodeType, node.NodeType))
                    {
                        return BadRequest(new { error = $"Invalid hierarchy: {parentNode.NodeType} cannot have {node.NodeType} as child. Valid hierarchy: Director → Manager → Inspector → Worker" });
                    }
                }

                node.Id = Guid.NewGuid().ToString();
                node.CreatedAt = DateTime.UtcNow;
                node.UpdatedAt = DateTime.UtcNow;

                _nodeService.AddNode(node);

                // Attach welcome message to the newly created node
                var createdNode = unitOfWork.Query<Node>().FirstOrDefault(n => n.Id == node.Id);
                if (createdNode != null)
                {
                    _nodeService.AttachWelcomeMessage(createdNode, user);
                    unitOfWork.CommitTransaction();
                }

                return CreatedAtAction(nameof(GetNode), new { id = node.Id }, node);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "CreateNode");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateNode(string id, [FromBody] NodeDto node)
        {
            try
            {
                if (node == null || node.Id != id) return BadRequest();

                var existingNode = _nodeService.GetNode(id);
                if (existingNode == null) return NotFound();

                node.UpdatedAt = DateTime.UtcNow;
                _nodeService.UpdateNode(node);
                return Ok(node);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "UpdateNode");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteNode(string id)
        {
            try
            {
                // Get authenticated user
                var user = UserService.GetUser(User, unitOfWork);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not authorized" });
                }

                var node = _nodeService.GetNode(id);
                if (node == null) return NotFound();

                // Attempt to delete the node (will throw exception if Director or unauthorized)
                _nodeService.DeleteNode(id, user);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // Director node deletion attempt
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "NodesController", "DeleteNode");
                return StatusCode(500, new { error = "An error occurred." });
            }
        }
    }
}
