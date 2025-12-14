using Microsoft.AspNetCore.Mvc;
using NodPT.Data.DTOs;
using NodPT.Data.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using RedisService.Queue;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService = new();
        private readonly RedisQueueService _redisService;
        private readonly ILogger<ChatController> _logger;
        private readonly UnitOfWork _session;

        public ChatController(RedisQueueService redisService, ILogger<ChatController> logger, UnitOfWork session)
        {
            _redisService = redisService;
            _logger = logger;
            _session = session;
        }

        [HttpGet("node/{nodeId}")]
        public IActionResult GetMessagesByNodeId(string nodeId)
        {
            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                var messages = _chatService.GetMessagesByNodeId(nodeId, user, _session);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized access to node: {nodeId}");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting messages for node: {nodeId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto userMessage)
        {
            if (userMessage == null) return BadRequest("Message cannot be null");
            if (string.IsNullOrEmpty(userMessage.NodeId)) return BadRequest("NodeId is required");

            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                // Get the connectionId from the DTO (should be sent by frontend)
                var connectionId = userMessage.ConnectionId;
                if (string.IsNullOrEmpty(connectionId))
                {
                    // Fallback to header for backward compatibility
                    connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
                }
                
                if (string.IsNullOrEmpty(connectionId))
                {
                    _logger.LogWarning("Missing SignalR ConnectionId in request");
                    return BadRequest(new { error = "ConnectionId is required" });
                }

                // Add user message to database with ConnectionId
                userMessage.Sender = "user";
                userMessage.ConnectionId = connectionId;
                var savedMessage = _chatService.AddMessage(userMessage, user, _session);

                // Ensure DB commit before publishing to Redis
                await _session.CommitChangesAsync();

                // Get node details for context (should exist after AddMessage creates it if needed)
                var node = _session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", userMessage.NodeId));

                // Prepare minimal envelope for Redis stream (jobs:chat)
                var envelope = new Dictionary<string, string>
                {
                    { "chatId", savedMessage.Oid.ToString() },
                };

                // Add to Redis stream for executor processing
                var entryId = await _redisService.Add("jobs:chat", envelope);

                _logger.LogInformation($"Chat message queued for processing: ChatId={savedMessage.Oid}, ConnectionId={connectionId}, EntryId={entryId}");

                return Ok(new 
                { 
                    userMessage = new ChatMessageDto
                    {
                        Id = savedMessage.Oid,
                        Sender = savedMessage.Sender,
                        Message = savedMessage.Message,
                        Timestamp = savedMessage.Timestamp,
                        NodeId = savedMessage.Node?.Id,
                        MarkedAsSolution = savedMessage.MarkedAsSolution,
                        Liked = savedMessage.Liked,
                        Disliked = savedMessage.Disliked,
                        ConnectionId = savedMessage.ConnectionId
                    },
                    status = "queued"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in SendMessage");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in SendMessage");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("mark-solution")]
        public IActionResult MarkAsSolution([FromBody] MarkSolutionRequestDto request)
        {
            if (request == null) return BadRequest("Request cannot be null");

            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                if (request.MessageId == null || request.MessageId == 0)
                {
                    return BadRequest(new { error = "MessageId is required" });
                }

                var message = _chatService.MarkAsSolution(request.MessageId.Value, user, _session);
                if (message == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                return Ok(new ChatMessageDto
                {
                    Id = message.Oid,
                    Sender = message.Sender,
                    Message = message.Message,
                    Timestamp = message.Timestamp,
                    NodeId = message.Node?.Id,
                    MarkedAsSolution = message.MarkedAsSolution,
                    Liked = message.Liked,
                    Disliked = message.Disliked
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in MarkAsSolution");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in MarkAsSolution");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkAsSolution");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("like")]
        public IActionResult LikeMessage([FromBody] ChatResponseDto chatResponse)
        {
            if (chatResponse == null) return BadRequest("ChatResponse cannot be null");
            
            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                var message = _chatService.UpdateMessageReaction(chatResponse.ChatMessageId, "like", user, _session);
                if (message == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                return Ok(new ChatMessageDto
                {
                    Id = message.Oid,
                    Sender = message.Sender,
                    Message = message.Message,
                    Timestamp = message.Timestamp,
                    NodeId = message.Node?.Id,
                    MarkedAsSolution = message.MarkedAsSolution,
                    Liked = message.Liked,
                    Disliked = message.Disliked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LikeMessage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("dislike")]
        public IActionResult DislikeMessage([FromBody] ChatResponseDto chatResponse)
        {
            if (chatResponse == null) return BadRequest("ChatResponse cannot be null");
            
            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                var message = _chatService.UpdateMessageReaction(chatResponse.ChatMessageId, "dislike", user, _session);
                if (message == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                return Ok(new ChatMessageDto
                {
                    Id = message.Oid,
                    Sender = message.Sender,
                    Message = message.Message,
                    Timestamp = message.Timestamp,
                    NodeId = message.Node?.Id,
                    MarkedAsSolution = message.MarkedAsSolution,
                    Liked = message.Liked,
                    Disliked = message.Disliked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DislikeMessage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
