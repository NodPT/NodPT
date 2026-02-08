using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class NodeService
    {
        private readonly UnitOfWork session;

        public NodeService(UnitOfWork unitOfWork)
        {
            this.session = unitOfWork;
        }

        /// <summary>
        /// Creates and attaches a welcome message to a node
        /// </summary>
        /// <param name="node">The node to attach the welcome message to</param>
        /// <param name="user">The user creating the message (optional)</param>
        /// <returns>The created ChatMessage</returns>
        public ChatMessage AttachWelcomeMessage(Node node, User? user = null)
        {
            // Define standard welcome messages
            var welcomeMessages = new[]
            {
                "Hello, how may I help you?",
                "Welcome! I'm ready to assist you.",
                "Hi there! What can I do for you today?",
                "Greetings! How can I be of service?"
            };

            // Select a welcome message (can use random or based on node type)
            var selectedMessage = welcomeMessages[Random.Shared.Next(welcomeMessages.Length)];
            // Create the welcome message
            var chatMessage = new ChatMessage(session)
            {
                Sender = "Assistant",
                Message = selectedMessage,
                Timestamp = DateTime.UtcNow,
                MarkedAsSolution = false,
                Liked = false,
                Disliked = false,
                Node = node,
                User = user
            };

            chatMessage.Save();
            return chatMessage;
        }

        private NodeDto MapToDto(Node node)
        {
            var dto = new NodeDto
            {
                Id = node.Id,
                Name = node.Name,
                NodeType = node.NodeType,
                Properties = node.PropertiesDictionary,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt,
                Status = node.Status,
                ParentId = node.Parent?.Id,
                OriginalParentNodeId = node.OriginalParentNodeId,
                ProjectId = node.Project?.Oid,
                ProjectName = node.Project?.Name,
                TemplateId = node.Template?.Oid,
                TemplateName = node.Template?.Name,
                MessageType = node.MessageType
            };

            return dto;
        }
        public static List<PromptDto> GetAIPromptsByNode(Node node)
        {

            // Map MatchingPrompts
            return node.GetMatchingPrompts().Select(p => new PromptDto
            {
                Id = p.Oid,
                Content = p.Content,
                MessageType = p.MessageType,
                NodeType = p.NodeType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TemplateId = p.Template?.Oid
            }).ToList();
        }

        public List<NodeDto> GetAllNodes()
        {
            var nodes = new XPCollection<Node>(session);
            return nodes.Select(n => MapToDto(n)).ToList();
        }

        public NodeDto? GetNode(string id)
        {
            var node = session.Query<Node>().FirstOrDefault(n => n.Id == id);
            return node == null ? null : MapToDto(node);
        }

        public List<NodeDto> GetNodesByProject(int projectId)
        {
            var project = session.GetObjectByKey<Project>(projectId);
            if (project == null) return new List<NodeDto>();

            var nodes = new XPCollection<Node>(session,
                new DevExpress.Data.Filtering.BinaryOperator("Project", project));
            return nodes.Select(n => MapToDto(n)).ToList();
        }

        public Node AddNode(NodeDto nodeDto)
        {
            session.BeginTransaction();

            try
            {
                var project = nodeDto.ProjectId.HasValue
                    ? session.GetObjectByKey<Project>(nodeDto.ProjectId.Value)
                    : null;
                var template = nodeDto.TemplateId.HasValue
                    ? session.GetObjectByKey<Template>(nodeDto.TemplateId.Value)
                    : null;
                var parent = !string.IsNullOrEmpty(nodeDto.ParentId)
                    ? session.Query<Node>().FirstOrDefault(n => n.Id == nodeDto.ParentId)
                    : null;

                var node = new Node(session)
                {
                    Id = nodeDto.Id,
                    Name = nodeDto.Name,
                    NodeType = nodeDto.NodeType,
                    PropertiesDictionary = nodeDto.Properties,
                    CreatedAt = nodeDto.CreatedAt,
                    UpdatedAt = nodeDto.UpdatedAt,
                    Status = nodeDto.Status,
                    Parent = parent,
                    Project = project,
                    Template = template,
                    MessageType = nodeDto.MessageType,
                    OriginalParentNodeId = nodeDto.OriginalParentNodeId
                };

                session.Save(node);
                session.CommitTransaction();

                return node;
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public void UpdateNode(NodeDto nodeDto)
        {
            session.BeginTransaction();

            try
            {
                var node = session.Query<Node>().FirstOrDefault(n => n.Id == nodeDto.Id);
                if (node == null) return;

                var project = nodeDto.ProjectId.HasValue
                    ? session.GetObjectByKey<Project>(nodeDto.ProjectId.Value)
                    : null;
                var template = nodeDto.TemplateId.HasValue
                    ? session.GetObjectByKey<Template>(nodeDto.TemplateId.Value)
                    : null;
                var parent = !string.IsNullOrEmpty(nodeDto.ParentId)
                    ? session.Query<Node>().FirstOrDefault(n => n.Id == nodeDto.ParentId)
                    : null;

                node.Name = nodeDto.Name;
                node.NodeType = nodeDto.NodeType;
                node.PropertiesDictionary = nodeDto.Properties;
                node.UpdatedAt = nodeDto.UpdatedAt;
                node.Status = nodeDto.Status;
                node.Parent = parent;
                node.Project = project;
                node.Template = template;
                node.MessageType = nodeDto.MessageType;
                node.OriginalParentNodeId = nodeDto.OriginalParentNodeId;

                session.Save(node);
                session.CommitTransaction();
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Soft-deletes a node by clearing its parent reference and storing the original parent ID.
        /// Also cascades the soft delete to all child nodes to prevent orphaning.
        /// Prevents deletion of Director nodes.
        /// </summary>
        /// <remarks>
        /// This operation soft-deletes the specified node and cascades to all its children.
        /// Both the node and its children have their parent references cleared and original
        /// parent IDs stored in OriginalParentNodeId for potential recovery.
        /// </remarks>
        /// <param name="id">Node ID to delete</param>
        /// <param name="user">User performing the deletion (for authorization)</param>
        /// <exception cref="InvalidOperationException">When attempting to delete a Director node</exception>
        /// <exception cref="UnauthorizedAccessException">When user doesn't own the node</exception>
        public void DeleteNode(string id, User? user = null)
        {
            session.BeginTransaction();

            try
            {
                var node = session.Query<Node>().FirstOrDefault(n => n.Id == id);
                if (node == null)
                {
                    session.RollbackTransaction();
                    throw new ArgumentException($"Node with ID '{id}' not found");
                }

                // Prevent deletion of Director nodes
                if (node.NodeType == NodeType.Director)
                {
                    session.RollbackTransaction();
                    throw new InvalidOperationException("Director nodes cannot be deleted");
                }

                // Verify user owns the node's project (if user is provided)
                if (user != null && node.Project?.User != null && node.Project.User.Oid != user.Oid)
                {
                    session.RollbackTransaction();
                    throw new UnauthorizedAccessException("You don't have permission to delete this node");
                }

                // Soft delete: Store original parent and clear parent reference
                if (node.Parent != null)
                {
                    node.OriginalParentNodeId = node.Parent.Id;
                    node.Parent = null;
                }

                // Also update children to prevent orphaning
                // Store their original parent and clear their parent reference
                foreach (var child in node.Children.ToList())
                {
                    child.OriginalParentNodeId = node.Id;
                    child.Parent = null;
                    child.UpdatedAt = DateTime.UtcNow;
                    session.Save(child);
                }

                node.UpdatedAt = DateTime.UtcNow;
                session.Save(node);
                session.CommitTransaction();
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Determines the child node type based on parent node type
        /// Following hierarchy: Director → Manager → Supervisor → Agent
        /// </summary>
        /// <param name="parentNodeType">Parent node type</param>
        /// <returns>Child node type or null if parent cannot have children</returns>
        public NodeType? GetChildNodeType(NodeType parentNodeType)
        {
            return parentNodeType switch
            {
                NodeType.Director => NodeType.Manager,
                NodeType.Manager => NodeType.Supervisor,
                NodeType.Supervisor => NodeType.Agent,
                _ => null // Agent and other types cannot have children
            };
        }
    }
}
