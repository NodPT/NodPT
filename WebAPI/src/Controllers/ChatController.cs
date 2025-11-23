using Microsoft.AspNetCore.Mvc;
using NodPT.Data.DTOs;
using NodPT.Data.Services;
using Microsoft.AspNetCore.Authorization;
using NodPT.API.Services;
using System.Text.Json;
using NodPT.Data.Models;
using DevExpress.Xpo;
using System.Security.Cryptography;
using System.Text;

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

        public ChatController(IRedisService redisService, ILogger<ChatController> logger)
        {
            _redisService = redisService;
            _logger = logger;
        }

        /// <summary>
        /// Convert integer Oid to deterministic Guid using MD5 hash
        /// Note: MD5 is used here only for deterministic ID conversion, not for security purposes
        /// </summary>
        private static Guid ConvertOidToGuid(int oid)
        {
            // Create a deterministic Guid from the integer Oid using MD5
            // MD5 is sufficient for this use case as we only need deterministic mapping, not cryptographic security
            using (var md5 = MD5.Create())
            {
                byte[] oidBytes = BitConverter.GetBytes(oid);
                byte[] hash = md5.ComputeHash(oidBytes);
                return new Guid(hash);
            }
        }

        [HttpGet]
        public IActionResult GetMessages() => Ok(_chatService.GetMessages());

        [HttpGet("node/{nodeId}")]
        public IActionResult GetMessagesByNodeId(string nodeId)
        {
            try
            {
                using var session = new UnitOfWork();
                var user = UserService.GetUser(User, session);
                
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                // Get messages from database with validation
                var messages = ChatService.GetMessagesByNodeIdFromDb(nodeId, user, session);

                // Convert to DTOs
                var messageDtos = messages.Select(m => new ChatMessageDto
                {
                    Id = ConvertOidToGuid(m.Oid),
                    Sender = m.Sender,
                    Message = m.Message,
                    Timestamp = m.Timestamp,
                    NodeId = m.Node?.Id,
                    MarkedAsSolution = m.MarkedAsSolution,
                    Liked = m.Liked,
                    Disliked = m.Disliked
                }).ToList();

                return Ok(messageDtos);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "ArgumentException getting messages by node ID");
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "UnauthorizedAccessException getting messages by node ID");
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages by node ID");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public IActionResult PostMessage([FromBody] ChatMessageDto message)
        {
            if (message == null) return BadRequest();

            _chatService.AddMessage(message);
            return Ok(message);
        }

        [HttpPost("send")]
        public IActionResult SendMessage([FromBody] ChatMessageDto userMessage)
        {
            if (userMessage == null) return BadRequest("Message cannot be null");

            // Add user message
            _chatService.AddMessage(userMessage);

            // Generate AI response
            var aiResponse = _chatService.GenerateAiResponse(userMessage);
            
            return Ok(new { userMessage, aiResponse });
        }

        [HttpPost("mark-solution")]
        public IActionResult MarkAsSolution([FromBody] MarkSolutionRequestDto request)
        {
            if (request == null) return BadRequest("Request cannot be null");

            // Mark the latest message as solution and generate comprehensive response
            var solutionResponse = _chatService.MarkAsSolutionAndRespond(request.NodeId);
            
            return Ok(solutionResponse);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateMessage(Guid id, [FromBody] ChatMessageDto message)
        {
            if (message == null) return BadRequest();

            message.Id = id;
            _chatService.UpdateMessage(message);
            return Ok(message);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMessage(Guid id)
        {
            _chatService.DeleteMessage(id);
            return NoContent();
        }

        [HttpPost("like")]
        public IActionResult LikeMessage([FromBody] ChatResponseDto chatResponse)
        {
            if (chatResponse == null) return BadRequest("ChatResponse cannot be null");
            
            chatResponse.Action = "like";
            var response = _chatService.AddChatResponse(chatResponse);
            return Ok(response);
        }

        [HttpPost("dislike")]
        public IActionResult DislikeMessage([FromBody] ChatResponseDto chatResponse)
        {
            if (chatResponse == null) return BadRequest("ChatResponse cannot be null");
            
            chatResponse.Action = "dislike";
            var response = _chatService.AddChatResponse(chatResponse);
            return Ok(response);
        }

        [HttpPost("regenerate")]
        public IActionResult RegenerateMessage([FromBody] ChatResponseDto chatResponse)
        {
            if (chatResponse == null) return BadRequest("ChatResponse cannot be null");
            
            chatResponse.Action = "regenerate";
            _chatService.AddChatResponse(chatResponse);
            
            // Generate new response
            var newMessage = _chatService.RegenerateResponse(chatResponse.ChatMessageId, null);
            return Ok(newMessage);
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
                using var session = new UnitOfWork();
                var user = UserService.GetUser(User, session);
                
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or not authorized" });
                }

                // Validate nodeId is provided
                if (string.IsNullOrEmpty(dto.NodeLevel))
                {
                    return BadRequest(new { error = "NodeId is required" });
                }

                // Find the node and validate ownership
                var node = session.FindObject<Node>(new DevExpress.Data.Filtering.BinaryOperator("Id", dto.NodeLevel));
                
                if (node == null)
                {
                    return NotFound(new { error = "Node not found" });
                }

                // Validate that the node belongs to a project owned by the user
                if (node.Project == null || node.Project.User == null || node.Project.User.Oid != user.Oid)
                {
                    return Unauthorized(new { error = "Node does not belong to a project owned by the current user" });
                }

                // Get the project ID
                var projectId = node.Project.Oid.ToString();

                // Determine model name from node or template
                string? modelName = null;
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

                // Set the model in the DTO
                dto.Model = modelName;
                dto.ProjectId = projectId;
                dto.UserId = user.FirebaseUid;

                // Save user message to database
                var chatMessage = ChatService.SaveMessageToDb(dto.NodeLevel, dto.Message ?? "", "user", user, session);

                // Prepare data for Redis with all required fields for SignalR
                var redisData = new
                {
                    FirebaseUid = user.FirebaseUid,
                    ConnectionId = dto.ConnectionId,
                    NodeId = dto.NodeLevel,
                    ProjectId = projectId,
                    Message = dto.Message,
                    Model = modelName,
                    ChatMessageId = chatMessage.Oid.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                // Push to Redis queue for executor
                var jobData = JsonSerializer.Serialize(redisData);
                await _redisService.ListRightPushAsync("chat.jobs", jobData);

                _logger.LogInformation($"Chat message queued for processing: UserId={user.FirebaseUid}, ConnectionId={dto.ConnectionId}, NodeId={dto.NodeLevel}, ProjectId={projectId}, Model={modelName}");

                return Ok(new { status = "queued", messageId = chatMessage.Oid.ToString(), projectId = projectId });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "ArgumentNullException submitting chat message");
                return BadRequest(new { error = "Invalid argument: " + ex.ParamName });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "UnauthorizedAccessException submitting chat message");
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "InvalidOperationException submitting chat message");
                return StatusCode(500, new { error = "Operation failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting chat message");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
