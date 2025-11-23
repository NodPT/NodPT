using NodPT.Data.DTOs;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace NodPT.Data.Services
{
    public class ChatService
    {
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

        /// <summary>
        /// Get chat messages for a specific node, ensuring the node belongs to a project owned by the user
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <param name="user">The current user</param>
        /// <param name="session">The database session</param>
        /// <returns>List of chat messages for the node</returns>
        public List<ChatMessageDto> GetMessagesByNodeId(string nodeId, User user, UnitOfWork session)
        {
            // Find the node
            var node = session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", nodeId));
            
            if (node == null)
            {
                throw new ArgumentException($"Node with ID '{nodeId}' not found");
            }

            // Verify the node belongs to a project owned by the user
            if (node.Project == null)
            {
                throw new UnauthorizedAccessException($"Node '{nodeId}' does not belong to any project");
            }

            if (node.Project.User == null || node.Project.User.Oid != user.Oid)
            {
                throw new UnauthorizedAccessException($"Node '{nodeId}' does not belong to a project owned by the current user");
            }

            // Get messages for this node
            var messages = new XPCollection<ChatMessage>(session, 
                CriteriaOperator.Parse("Node.Id = ?", nodeId));

            return messages.OrderBy(m => m.Timestamp).Select(m => new ChatMessageDto
            {
                Id = m.Oid,
                Sender = m.Sender,
                Message = m.Message,
                Timestamp = m.Timestamp,
                MarkedAsSolution = m.MarkedAsSolution,
                NodeId = m.Node?.Id,
                Liked = m.Liked,
                Disliked = m.Disliked
            }).ToList();
        }

        [Obsolete("Use GetMessagesByNodeId with user validation instead")]
        public List<ChatMessageDto> GetMessages() => new List<ChatMessageDto>();

        /// <summary>
        /// Add a chat message to the database
        /// </summary>
        public ChatMessage AddMessage(ChatMessageDto messageDto, User user, UnitOfWork session)
        {
            var chatMessage = new ChatMessage(session)
            {
                Sender = messageDto.Sender,
                Message = messageDto.Message,
                Timestamp = DateTime.UtcNow,
                MarkedAsSolution = messageDto.MarkedAsSolution,
                Liked = false,
                Disliked = false,
                User = user
            };

            // If nodeId is provided, associate the message with the node
            if (!string.IsNullOrEmpty(messageDto.NodeId))
            {
                var node = session.FindObject<Node>(CriteriaOperator.Parse("Id = ?", messageDto.NodeId));
                if (node == null)
                {
                    throw new ArgumentException($"Node with ID '{messageDto.NodeId}' not found");
                }

                // Verify the node belongs to a project owned by the user
                if (node.Project == null || node.Project.User == null || node.Project.User.Oid != user.Oid)
                {
                    throw new UnauthorizedAccessException($"Node '{messageDto.NodeId}' does not belong to a project owned by the current user");
                }

                chatMessage.Node = node;
            }

            chatMessage.Save();
            return chatMessage;
        }

        /// <summary>
        /// Generate AI response (for backward compatibility, but messages should be queued to Redis)
        /// </summary>
        [Obsolete("Use Redis queuing instead for AI responses")]
        public ChatMessageDto GenerateAiResponse(ChatMessageDto userMessage)
        {
            var responseText = _aiResponses[_random.Next(_aiResponses.Count)];
            
            return new ChatMessageDto
            {
                Id = 0, // Will be assigned by database
                Sender = "ai",
                Message = responseText,
                Timestamp = DateTime.UtcNow,
                NodeId = userMessage.NodeId,
                MarkedAsSolution = false,
                Liked = false,
                Disliked = false
            };
        }

        /// <summary>
        /// Mark a message as solution
        /// </summary>
        public ChatMessage? MarkAsSolution(int messageId, User user, UnitOfWork session)
        {
            var message = session.GetObjectByKey<ChatMessage>(messageId);
            if (message == null)
            {
                throw new ArgumentException($"Message with ID '{messageId}' not found");
            }

            // Verify the message belongs to a node in a project owned by the user
            if (message.Node?.Project == null || message.Node.Project.User == null || message.Node.Project.User.Oid != user.Oid)
            {
                throw new UnauthorizedAccessException("Message does not belong to a project owned by the current user");
            }

            message.MarkedAsSolution = true;
            message.Save();
            return message;
        }

        /// <summary>
        /// Update a chat message's like/dislike status
        /// </summary>
        public ChatMessage? UpdateMessageReaction(int messageId, string action, User user, UnitOfWork session)
        {
            var message = session.GetObjectByKey<ChatMessage>(messageId);
            if (message == null)
            {
                throw new ArgumentException($"Message with ID '{messageId}' not found");
            }

            // Verify the message belongs to a node in a project owned by the user
            if (message.Node?.Project == null || message.Node.Project.User == null || message.Node.Project.User.Oid != user.Oid)
            {
                throw new UnauthorizedAccessException("Message does not belong to a project owned by the current user");
            }

            switch (action?.ToLower())
            {
                case "like":
                    message.Liked = !message.Liked;
                    message.Disliked = false;
                    break;
                case "dislike":
                    message.Disliked = !message.Disliked;
                    message.Liked = false;
                    break;
            }

            message.Save();
            return message;
        }
    }
}
