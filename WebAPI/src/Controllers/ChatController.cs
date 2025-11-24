using Microsoft.AspNetCore.Mvc;
using NodPT.Data.DTOs;
using NodPT.Data.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService = new();
        private readonly IRedisService _redisService;
        private readonly ILogger<ChatController> _logger;
        private readonly UnitOfWork _session;

        public ChatController(IRedisService redisService, ILogger<ChatController> logger, UnitOfWork session)
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

                // Get the connectionId from request headers (should be sent by frontend)
                var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
                if (string.IsNullOrEmpty(connectionId))
                {
                    _logger.LogWarning("Missing SignalR ConnectionId in request");
                }

                // Add user message to database
                userMessage.Sender = "user";
                var savedMessage = _chatService.AddMessage(userMessage, user, _session);

                // Get node and project details for Redis payload
                var node = _session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", userMessage.NodeId));
                if (node == null)
                {
                    return NotFound(new { error = "Node not found" });
                }

                // Prepare data for Redis queue with all required fields for SignalR
                var redisPayload = new
                {
                    UserId = user.FirebaseUid,
                    ConnectionId = connectionId,
                    NodeId = userMessage.NodeId,
                    ProjectId = node.Project?.Oid.ToString(),
                    Message = userMessage.Message,
                    ChatMessageId = savedMessage.Oid.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                // Push to Redis queue for AI processing
                var jobData = JsonSerializer.Serialize(redisPayload);
                await _redisService.ListRightPushAsync("chat.jobs", jobData);

                _logger.LogInformation($"Chat message queued for processing: UserId={user.FirebaseUid}, NodeId={userMessage.NodeId}, MessageId={savedMessage.Oid}");

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
                        Disliked = savedMessage.Disliked
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

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] ChatSubmitDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Chat submit data cannot be null");
            }

            try
            {
                var user = UserService.GetUser(User, _session);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                // Determine model name from node or template
                string? modelName = null;
                if (!string.IsNullOrEmpty(dto.NodeLevel))
                {
                    var node = _session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", dto.NodeLevel));
                    
                    if (node != null)
                    {
                        // First check if node has a direct AIModel
                        if (node.AIModel != null && !string.IsNullOrEmpty(node.AIModel.ModelIdentifier))
                        {
                            modelName = node.AIModel.ModelIdentifier;
                            _logger.LogInformation($"Using model from node's AIModel: {modelName}");
                        }
                        // Otherwise, check for matching AIModel from template
                        else if (node.MatchingAIModel != null && !string.IsNullOrEmpty(node.MatchingAIModel.ModelIdentifier))
                        {
                            modelName = node.MatchingAIModel.ModelIdentifier;
                            _logger.LogInformation($"Using model from template's matching AIModel: {modelName}");
                        }
                    }
                }

                // Set the model in the DTO
                dto.Model = modelName;

                // Save to DB
                var chatMessageDto = new ChatMessageDto
                {
                    Sender = "user",
                    Message = dto.Message,
                    NodeId = dto.NodeLevel,
                    MarkedAsSolution = false
                };
                var chatMessage = _chatService.AddMessage(chatMessageDto, user, _session);

                // Push to Redis queue for executor
                var jobData = JsonSerializer.Serialize(dto);
                await _redisService.ListRightPushAsync("chat.jobs", jobData);

                _logger.LogInformation($"Chat message queued for processing: UserId={dto.UserId}, ConnectionId={dto.ConnectionId}, Model={modelName}");

                return Ok(new { status = "queued", messageId = chatMessage.Oid });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "ArgumentNullException submitting chat message");
                return BadRequest(new { error = "Invalid argument: " + ex.ParamName });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "InvalidOperationException submitting chat message");
                return StatusCode(500, new { error = "Operation failed: " + ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in Submit");
                return Forbid();
            }
        }
    }
}
