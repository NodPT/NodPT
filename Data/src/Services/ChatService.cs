using NodPT.Data.DTOs;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace NodPT.Data.Services
{
    public class ChatService
    {
        private static List<ChatMessageDto> _messages = new();
        private static readonly List<string> _aiResponses = new()
        {
            "I understand your request. Let me analyze this for you...",
            "That's an interesting question! Here's what I think...",
            "Based on the information provided, I suggest...",
            "I can help you with that. Consider the following approach...",
            "Let me process this request and provide you with a solution...",
            "Thank you for that input. Here's my analysis...",
            "I see what you're looking for. Here's a potential solution...",
            "That's a great question! Let me break this down for you...",
            "I've analyzed your request and here are my recommendations...",
            "Based on my understanding, here's what I would suggest..."
        };

        private static readonly List<string> _comprehensiveSolutions = new()
        {
            "Here's a comprehensive solution to your problem:\n\n1. First, analyze the current workflow structure\n2. Identify bottlenecks and optimization opportunities\n3. Implement automated processes where possible\n4. Monitor and iterate based on performance metrics\n5. Document the changes for future reference\n\nThis approach should significantly improve your workflow efficiency.",
            "I've prepared a detailed solution for you:\n\n**Analysis Phase:**\n- Review current node connections\n- Identify data flow patterns\n- Assess performance requirements\n\n**Implementation Phase:**\n- Restructure node hierarchy\n- Optimize data processing\n- Add error handling mechanisms\n\n**Testing Phase:**\n- Validate workflow functionality\n- Performance testing\n- User acceptance testing\n\nThis comprehensive approach ensures a robust solution.",
            "Here's a complete solution strategy:\n\n**Problem Assessment:**\nI've identified the key challenges in your current setup and developed a multi-layered approach to address them.\n\n**Recommended Solution:**\n1. Implement modular architecture\n2. Add real-time monitoring\n3. Create backup and recovery procedures\n4. Establish quality gates\n5. Set up continuous integration\n\n**Expected Outcomes:**\n- Improved reliability\n- Better performance\n- Enhanced maintainability\n- Reduced operational overhead",
            "Complete solution framework:\n\n**Technical Implementation:**\n- Refactor current codebase for better modularity\n- Implement dependency injection patterns\n- Add comprehensive logging and monitoring\n- Create automated testing suites\n\n**Process Improvements:**\n- Establish code review practices\n- Implement continuous deployment\n- Add performance benchmarking\n- Create documentation standards\n\n**Quality Assurance:**\n- Set up automated testing pipelines\n- Implement security scanning\n- Add compliance checking\n- Create rollback procedures\n\nThis solution addresses both immediate needs and long-term scalability."
        };

        private static readonly Random _random = new();

        public List<ChatMessageDto> GetMessages() => _messages;

        public List<ChatMessageDto> GetMessagesByNodeId(string nodeId) =>
            _messages.Where(m => m.NodeId == nodeId).ToList();

        public void AddMessage(ChatMessageDto message)
        {
            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;
            _messages.Add(message);
        }

        public ChatMessageDto GenerateAiResponse(ChatMessageDto userMessage)
        {
            // Generate a randomized AI response
            var responseText = _aiResponses[_random.Next(_aiResponses.Count)];
            
            var aiResponse = new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                Sender = "ai",
                Message = responseText,
                Timestamp = DateTime.UtcNow,
                NodeId = userMessage.NodeId,
                MarkedAsSolution = false,
                Liked = false,
                Disliked = false
            };

            _messages.Add(aiResponse);
            return aiResponse;
        }

        public ChatMessageDto MarkAsSolutionAndRespond(string? nodeId)
        {
            // Find the latest AI message for this node and mark it as solution
            var latestAiMessage = _messages
                .Where(m => m.Sender == "ai" && (nodeId == null || m.NodeId == nodeId))
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();

            if (latestAiMessage != null)
            {
                latestAiMessage.MarkedAsSolution = true;
                UpdateMessage(latestAiMessage);
            }

            // Generate a comprehensive solution response
            var comprehensiveResponse = _comprehensiveSolutions[_random.Next(_comprehensiveSolutions.Count)];
            
            var solutionMessage = new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                Sender = "ai",
                Message = comprehensiveResponse,
                Timestamp = DateTime.UtcNow,
                NodeId = nodeId,
                MarkedAsSolution = true,
                Liked = false,
                Disliked = false
            };

            _messages.Add(solutionMessage);
            return solutionMessage;
        }

        public void UpdateMessage(ChatMessageDto message)
        {
            var existingMessage = _messages.FirstOrDefault(m => m.Id.Equals(message.Id));
            if (existingMessage != null)
            {
                var index = _messages.IndexOf(existingMessage);
                _messages[index] = message;
            }
        }

        public void DeleteMessage(Guid messageId)
        {
            var message = _messages.FirstOrDefault(m => m.Id.Equals(messageId));
            if (message != null)
            {
                _messages.Remove(message);
            }
        }

        public ChatResponseDto AddChatResponse(ChatResponseDto chatResponse)
        {
            chatResponse.Id = Guid.NewGuid();
            chatResponse.Timestamp = DateTime.UtcNow;

            // Update the message like/dislike status based on the action
            var message = _messages.FirstOrDefault(m => m.Id.Equals(chatResponse.ChatMessageId));
            if (message != null)
            {
                switch (chatResponse.Action?.ToLower())
                {
                    case "like":
                        message.Liked = true;
                        message.Disliked = false; // Can't be both liked and disliked
                        break;
                    case "dislike":
                        message.Disliked = true;
                        message.Liked = false; // Can't be both liked and disliked
                        break;
                }
            }

            return chatResponse;
        }

        public ChatMessageDto RegenerateResponse(Guid originalMessageId, string? nodeId)
        {
            // Find the original message
            var originalMessage = _messages.FirstOrDefault(m => m.Id.Equals(originalMessageId));
            if (originalMessage == null)
            {
                throw new ArgumentException("Original message not found");
            }

            // Generate a new AI response with different content
            var responseText = _aiResponses[_random.Next(_aiResponses.Count)];
            
            var newResponse = new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                Sender = "ai",
                Message = $"[Regenerated] {responseText}",
                Timestamp = DateTime.UtcNow,
                NodeId = nodeId ?? originalMessage.NodeId,
                MarkedAsSolution = false,
                Liked = false,
                Disliked = false
            };

            _messages.Add(newResponse);
            return newResponse;
        }

        /// <summary>
        /// Validate node ownership - ensures the node belongs to a project owned by the user
        /// </summary>
        private static void ValidateNodeOwnership(Node node, User user)
        {
            if (node == null)
            {
                throw new ArgumentException("Node not found");
            }

            if (node.Project == null || node.Project.User == null || node.Project.User.Oid != user.Oid)
            {
                throw new UnauthorizedAccessException("Node does not belong to a project owned by the current user");
            }
        }

        /// <summary>
        /// Get chat messages from database by nodeId, ensuring the node belongs to a project owned by the user
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <param name="user">The authenticated user</param>
        /// <param name="session">The database session</param>
        /// <returns>List of chat messages for the node</returns>
        public static List<ChatMessage> GetMessagesByNodeIdFromDb(string nodeId, User user, UnitOfWork session)
        {
            // Find the node
            var node = session.FindObject<Node>(new BinaryOperator("Id", nodeId));
            
            // Validate node ownership
            ValidateNodeOwnership(node, user);

            // Get all chat messages for this node
            var messages = new XPCollection<ChatMessage>(session, 
                new BinaryOperator("Node", node));

            return messages.OrderBy(m => m.Timestamp).ToList();
        }

        /// <summary>
        /// Save a chat message to the database
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <param name="message">The message content</param>
        /// <param name="sender">The sender (user or ai)</param>
        /// <param name="user">The authenticated user</param>
        /// <param name="session">The database session</param>
        /// <returns>The saved chat message</returns>
        public static ChatMessage SaveMessageToDb(string nodeId, string message, string sender, User user, UnitOfWork session)
        {
            // Find the node
            var node = session.FindObject<Node>(new BinaryOperator("Id", nodeId));
            
            // Validate node ownership
            ValidateNodeOwnership(node, user);

            // Create and save the chat message
            var chatMessage = new ChatMessage(session)
            {
                Sender = sender,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Node = node,
                User = user,
                MarkedAsSolution = false,
                Liked = false,
                Disliked = false
            };

            chatMessage.Save();
            session.CommitChanges();

            return chatMessage;
        }
    }
}
