using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NodPT.Data.Models;
using NodPT.Data;
using Microsoft.EntityFrameworkCore;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly NodPTDbContext _context;

        public ChatMessagesController(NodPTDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [CustomAuthorized("Admin")]
        public IActionResult GetAllChatMessages()
        {
            var messages = _context.ChatMessages
                .Include(m => m.Node)
                .Include(m => m.User)
                .ToList();
            
            // Project to DTOs to avoid serialization issues
            var messageDtos = messages.Select(m => new
            {
                m.Id,
                m.Sender,
                m.Message,
                m.Timestamp,
                m.MarkedAsSolution,
                m.Liked,
                m.Disliked,
                NodeId = m.Node?.Id,
                NodeName = m.Node?.Name,
                UserFirebaseUid = m.User?.FirebaseUid,
                UserDisplayName = m.User?.DisplayName
            }).ToList();
            
            return Ok(messageDtos);
        }

        [HttpGet("me")]
        public IActionResult GetMyChatMessages()
        {
            // Get user from token using EF Core context
            var user = UserService.GetUser(User, _context);
            if (user == null) return Unauthorized(new { error = "User not found or invalid" });
            
            var messages = _context.ChatMessages
                .Include(m => m.Node)
                .Where(m => m.UserId == user.Id)
                .ToList();
            
            var messageDtos = messages.Select(m => new
            {
                m.Id,
                m.Sender,
                m.Message,
                m.Timestamp,
                m.MarkedAsSolution,
                m.Liked,
                m.Disliked,
                NodeId = m.Node?.Id,
                NodeName = m.Node?.Name
            }).ToList();
            
            return Ok(messageDtos);
        }

        [HttpGet("node/{nodeId}")]
        
        public IActionResult GetChatMessagesByNode(string nodeId)
        {
            var node = _context.Nodes.FirstOrDefault(n => n.Id == nodeId);
            
            if (node == null) return NotFound("Node not found");
            
            var messages = _context.ChatMessages
                .Include(m => m.Node)
                .Include(m => m.User)
                .Where(m => m.NodeId == nodeId)
                .OrderBy(m => m.Timestamp)
                .ToList();
            
            var messageDtos = messages.Select(m => new
            {
                m.Id,
                m.Sender,
                m.Message,
                m.Timestamp,
                m.MarkedAsSolution,
                m.Liked,
                m.Disliked,
                NodeId = m.Node?.Id,
                NodeName = m.Node?.Name,
                UserFirebaseUid = m.User?.FirebaseUid,
                UserDisplayName = m.User?.DisplayName
            }).ToList();
            
            return Ok(messageDtos);
        }
    }
}