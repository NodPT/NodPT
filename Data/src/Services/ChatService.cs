using NodPT.Data.DTOs;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace NodPT.Data.Services
{
    public class ChatService
    {

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
                    if (message.Liked)
                    {
                        // If already liked, remove the like (unlike)
                        message.Liked = false;
                        // Do not touch Disliked
                    }
                    else
                    {
                        // If disliked or neutral, set like and clear dislike
                        message.Liked = true;
                        message.Disliked = false;
                    }
                    break;
                case "dislike":
                    if (message.Disliked)
                    {
                        // If already disliked, remove the dislike (undislike)
                        message.Disliked = false;
                        // Do not touch Liked
                    }
                    else
                    {
                        // If liked or neutral, set dislike and clear like
                        message.Disliked = true;
                        message.Liked = false;
                    }
                    break;
            }

            message.Save();
            return message;
        }
    }
}
