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

        /// <summary>
        /// Helper method to retrieve connectionId from DTO or HTTP header
        /// </summary>
        private string? GetConnectionId(string? dtoConnectionId)
        {
            if (!string.IsNullOrEmpty(dtoConnectionId))
            {
                return dtoConnectionId;
            }
            
            // Fallback to header for backward compatibility
            return Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Node not found: {nodeId}");
                return NotFound(new { error = ex.Message });
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
                var connectionId = GetConnectionId(userMessage.ConnectionId);

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


                // Prepare minimal envelope for Redis stream (jobs:chat)
                var envelope = new Dictionary<string, string>
                {
                    { "chatId", savedMessage.Oid.ToString() },
                };

                // Add to Redis stream for executor processing
                var entryId = await _redisService.Add("jobs:chat", envelope);

                _logger.LogInformation("Chat message queued for processing: ChatId={ChatId}, ConnectionId={ConnectionId}, EntryId={EntryId}", savedMessage.Oid, connectionId, entryId);

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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in SendMessage");
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
        public async Task<IActionResult> MarkAsSolution([FromBody] MarkSolutionRequestDto request)
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

                // Get connectionId from request or header (optional)
                var connectionId = GetConnectionId(request.ConnectionId);

                if (string.IsNullOrEmpty(connectionId))
                {
                    // Fallback to existing connectionId from the message (if any)
                    connectionId = message.ConnectionId;
                    
                    if (string.IsNullOrEmpty(connectionId))
                    {
                        _logger.LogWarning("Missing SignalR ConnectionId when marking as solution - real-time notifications will not be sent");
                    }
                }
                else
                {
                    // Update the message with the current connectionId to ensure proper delivery
                    message.ConnectionId = connectionId;
                }
                
                // Commit the solution marking and any connectionId update in a single transaction
                await _session.CommitChangesAsync();

                var solutionEnvelope = new Dictionary<string, string>
                {
                    { "chatId", message.Oid.ToString() },
                    { "jobType", "solution" }
                };

                var entryId = await _redisService.Add("jobs:chat", solutionEnvelope);
                _logger.LogInformation("Solution chat queued for processing: ChatId={ChatId}, ConnectionId={ConnectionId}, EntryId={EntryId}", message.Oid, connectionId, entryId);

                return Ok(new ChatMessageDto
                {
                    Id = message.Oid,
                    Sender = message.Sender,
                    Message = message.Message,
                    Timestamp = message.Timestamp,
                    NodeId = message.Node?.Id,
                    MarkedAsSolution = message.MarkedAsSolution,
                    Liked = message.Liked,
                    Disliked = message.Disliked,
                    ConnectionId = message.ConnectionId
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
                    Disliked = message.Disliked,
                    ConnectionId = message.ConnectionId
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
                    Disliked = message.Disliked,
                    ConnectionId = message.ConnectionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DislikeMessage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("retry")]
        public async Task<IActionResult> RetryMessage([FromBody] RetryMessageRequestDto request)
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
                    // MessageId 0 is also invalid since database IDs start from 1
                    return BadRequest(new { error = "MessageId is required" });
                }

                var message = _chatService.GetMessageForRetry(request.MessageId.Value, user, _session);
                if (message == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                // Get connectionId from request or header
                var connectionId = GetConnectionId(request.ConnectionId);

                if (string.IsNullOrEmpty(connectionId))
                {
                    // Fallback to existing connectionId from the message (if any)
                    connectionId = message.ConnectionId;
                    
                    if (string.IsNullOrEmpty(connectionId))
                    {
                        _logger.LogWarning("Missing SignalR ConnectionId when retrying message - real-time notifications will not be sent");
                    }
                }
                else
                {
                    // Update the message with the current connectionId to ensure proper delivery
                    message.ConnectionId = connectionId;
                }
                
                // Commit any connectionId update
                await _session.CommitChangesAsync();

                // Queue the retry job to Redis with retry flag
                var retryEnvelope = new Dictionary<string, string>
                {
                    { "chatId", message.Oid.ToString() },
                    { "jobType", "retry" }
                };

                var entryId = await _redisService.Add("jobs:chat", retryEnvelope);
                _logger.LogInformation("Retry chat queued for processing: ChatId={ChatId}, ConnectionId={ConnectionId}, EntryId={EntryId}", message.Oid, connectionId, entryId);

                return Ok(new ChatMessageDto
                {
                    Id = message.Oid,
                    Sender = message.Sender,
                    Message = message.Message,
                    Timestamp = message.Timestamp,
                    NodeId = message.Node?.Id,
                    MarkedAsSolution = message.MarkedAsSolution,
                    Liked = message.Liked,
                    Disliked = message.Disliked,
                    ConnectionId = message.ConnectionId
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in RetryMessage");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in RetryMessage");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetryMessage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
