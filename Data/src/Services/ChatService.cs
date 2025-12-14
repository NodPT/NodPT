using NodPT.Data.DTOs;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace NodPT.Data.Services
{
    public class ChatService
    {
        /// <summary>
        /// Extract a meaningful name from a node ID
        /// Format: node_<parent>_<name>_<timestamp>_<random>
        /// </summary>
        private string ExtractNodeNameFromId(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return "Unknown Node";

            var parts = nodeId.Split('_');
            
            // If we have at least 3 parts (node, parent, name, ...), extract the name
            if (parts.Length >= 3)
            {
                // The name is typically the 3rd part (index 2)
                return parts[2];
            }
            
            // Fallback to using the full ID
            return nodeId;
        }

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
            
            // If node doesn't exist in database yet, return empty list
            // This allows frontend to work with nodes that haven't been persisted
            if (node == null)
            {
                return new List<ChatMessageDto>();
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
                
                // If node doesn't exist, create it
                if (node == null)
                {
                    // Get the user's active project using XPO query for better performance
                    var userProject = session.FindObject<Project>(
                        CriteriaOperator.Parse("User.Oid = ? AND IsActive = ?", user.Oid, true)
                    );
                    
                    if (userProject == null)
                    {
                        throw new InvalidOperationException($"Cannot create node '{messageDto.NodeId}': User has no active project");
                    }

                    // Extract a more meaningful name from the node ID
                    // Format: node_<parent>_<name>_<timestamp>_<random>
                    var nodeName = ExtractNodeNameFromId(messageDto.NodeId);

                    node = new Node(session)
                    {
                        Id = messageDto.NodeId,
                        Name = nodeName,
                        NodeType = NodeType.Default,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "active",
                        Project = userProject
                    };
                    node.Save();
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
