using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NodPT.Data.Models;
using NodPT.Data.Services;
using DevExpress.Xpo;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly UnitOfWork session;

        public ChatMessagesController(UnitOfWork _unitOfWork)
        {
            this.session = _unitOfWork;
        }

        [HttpGet]
        [CustomAuthorized("Admin")]
        public IActionResult GetAllChatMessages()
        {
            var messages = new XPCollection<ChatMessage>(session);
            
            // Project to DTOs to avoid serialization issues
            var messageDtos = messages.Select(m => new
            {
                m.Oid,
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

        [HttpGet("user/me")]
        public IActionResult GetMyChatMessages()
        {
            var user = UserService.GetUser(User, session);
            
            if (user == null) 
                return Unauthorized(new { error = "User not found or not authorized" });
            
            var messageDtos = session.Query<ChatMessage>()
                .Where(m => m.User == user)
                .Select(m => new
                {
                    m.Oid,
                    m.Sender,
                    m.Message,
                    m.Timestamp,
                    m.MarkedAsSolution,
                    m.Liked,
                    m.Disliked,
                    NodeId = m.Node != null ? m.Node.Id : null,
                    NodeName = m.Node != null ? m.Node.Name : null
                }).ToList();
            
            return Ok(messageDtos);
        }

        [HttpGet("user/{firebaseUid}")]
        [CustomAuthorized("Admin")]
        public IActionResult GetChatMessagesByUser(string firebaseUid)
        {
            var user = session.FindObject<User>(new DevExpress.Data.Filtering.BinaryOperator("FirebaseUid", firebaseUid));
            
            if (user == null) return NotFound("User not found");
            
            var messages = new XPCollection<ChatMessage>(session, 
                new DevExpress.Data.Filtering.BinaryOperator("User", user));
            
            var messageDtos = messages.Select(m => new
            {
                m.Oid,
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
            var node = session.FindObject<Node>(new DevExpress.Data.Filtering.BinaryOperator("Id", nodeId));
            
            if (node == null) return NotFound("Node not found");
            
            var messages = new XPCollection<ChatMessage>(session, 
                new DevExpress.Data.Filtering.BinaryOperator("Node", node));
            
            var messageDtos = messages.OrderBy(m => m.Timestamp).Select(m => new
            {
                m.Oid,
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