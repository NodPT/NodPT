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

                // Get node details for context
                var node = _session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", userMessage.NodeId));
                if (node == null)
                {
                    return NotFound(new { error = "Node not found" });
                }

                // Prepare minimal envelope for Redis stream (jobs:chat)
                var envelope = new Dictionary<string, string>
                {
                    { "chatId", savedMessage.Oid.ToString() },
                    { "connectionId", connectionId },
                    { "nodeId", userMessage.NodeId },
                    { "userId", user.FirebaseUid ?? "" },
                    { "projectId", node.Project?.Oid.ToString() ?? "" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
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

            if (string.IsNullOrEmpty(dto.ConnectionId))
            {
                return BadRequest("ConnectionId is required");
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
                Node? node = null;
                if (!string.IsNullOrEmpty(dto.NodeLevel))
                {
                    node = _session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", dto.NodeLevel));
                    
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

                // Save to DB with ConnectionId
                var chatMessageDto = new ChatMessageDto
                {
                    Sender = "user",
                    Message = dto.Message,
                    NodeId = dto.NodeLevel,
                    MarkedAsSolution = false,
                    ConnectionId = dto.ConnectionId
                };
                var chatMessage = _chatService.AddMessage(chatMessageDto, user, _session);

                // Ensure DB commit before publishing to Redis
                await _session.CommitChangesAsync();

                // Prepare minimal envelope for Redis stream (jobs:chat)
                var envelope = new Dictionary<string, string>
                {
                    { "chatId", chatMessage.Oid.ToString() },
                    { "connectionId", dto.ConnectionId },
                    { "nodeId", dto.NodeLevel ?? "" },
                    { "userId", user.FirebaseUid ?? dto.UserId ?? "" },
                    { "projectId", node?.Project?.Oid.ToString() ?? dto.ProjectId ?? "" },
                    { "model", modelName ?? "" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                // Add to Redis stream for executor processing
                var entryId = await _redisService.Add("jobs:chat", envelope);

                _logger.LogInformation($"Chat message queued for processing: ChatId={chatMessage.Oid}, ConnectionId={dto.ConnectionId}, Model={modelName}, EntryId={entryId}");

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
