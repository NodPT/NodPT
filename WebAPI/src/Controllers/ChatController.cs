using Microsoft.AspNetCore.Mvc;
using NodPT.Data.DTOs;
using NodPT.Data.Services;
using Microsoft.AspNetCore.Authorization;
using NodPT.API.Services;
using System.Text.Json;

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

        [HttpGet]
        public IActionResult GetMessages() => Ok(_chatService.GetMessages());

        [HttpGet("node/{nodeId}")]
        public IActionResult GetMessagesByNodeId(string nodeId) =>
            Ok(_chatService.GetMessagesByNodeId(nodeId));

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
                // Save to DB (using in-memory ChatService for now)
                var chatMessage = new ChatMessageDto
                {
                    Id = Guid.NewGuid(),
                    Sender = "user",
                    Message = dto.Message,
                    Timestamp = DateTime.UtcNow,
                    NodeId = dto.NodeLevel,
                    MarkedAsSolution = false
                };
                _chatService.AddMessage(chatMessage);

                // Push to Redis queue for executor
                var jobData = JsonSerializer.Serialize(dto);
                await _redisService.ListRightPushAsync("chat.jobs", jobData);

                _logger.LogInformation($"Chat message queued for processing: UserId={dto.UserId}, ConnectionId={dto.ConnectionId}");

                return Ok(new { status = "queued", messageId = chatMessage.Id });
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
        }
    }
}
