using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class NodeService
    {
        private NodeDto MapToDto(Node node)
        {
            var dto = new NodeDto
            {
                Id = node.Id,
                Name = node.Name,
                NodeType = node.NodeType.ToString(),
                Properties = node.PropertiesDictionary,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt,
                Status = node.Status,
                ParentId = node.Parent?.Id,
                ProjectId = node.Project?.Oid,
                ProjectName = node.Project?.Name,
                TemplateId = node.Template?.Oid,
                TemplateName = node.Template?.Name,
                MessageType = node.MessageType,
                Level = node.Level,
                AIModelId = node.AIModel?.Oid,
                AIModelName = node.AIModel?.Name
            };

            // Map MatchingAIModel if available
            if (node.MatchingAIModel != null)
            {
                dto.MatchingAIModel = new AIModelDto
                {
                    Id = node.MatchingAIModel.Oid,
                    Name = node.MatchingAIModel.Name,
                    ModelIdentifier = node.MatchingAIModel.ModelIdentifier,
                    MessageType = node.MatchingAIModel.MessageType,
                    Level = node.MatchingAIModel.Level,
                    Description = node.MatchingAIModel.Description,
                    IsActive = node.MatchingAIModel.IsActive,
                    CreatedAt = node.MatchingAIModel.CreatedAt,
                    UpdatedAt = node.MatchingAIModel.UpdatedAt,
                    TemplateId = node.MatchingAIModel.Template?.Oid
                };
            }

            // Map MatchingPrompts
            dto.MatchingPrompts = node.MatchingPrompts.Select(p => new PromptDto
            {
                Id = p.Oid,
                Content = p.Content,
                MessageType = p.MessageType,
                Level = p.Level,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TemplateId = p.Template?.Oid
            }).ToList();

            return dto;
        }

        public List<NodeDto> GetAllNodes()
        {
            using var session = new Session();
            var nodes = new XPCollection<Node>(session);
            return nodes.Select(n => MapToDto(n)).ToList();
        }

        public NodeDto? GetNode(string id)
        {
            using var session = new Session();
            var node = session.Query<Node>().FirstOrDefault(n => n.Id == id);
            return node == null ? null : MapToDto(node);
        }

        public List<NodeDto> GetNodesByProject(int projectId)
        {
            using var session = new Session();
            var project = session.GetObjectByKey<Project>(projectId);
            if (project == null) return new List<NodeDto>();
            
            var nodes = new XPCollection<Node>(session, 
                new DevExpress.Data.Filtering.BinaryOperator("Project", project));
            return nodes.Select(n => MapToDto(n)).ToList();
        }

        public void AddNode(NodeDto nodeDto)
        {
            using var session = new Session();
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
                var aiModel = nodeDto.AIModelId.HasValue
                    ? session.GetObjectByKey<AIModel>(nodeDto.AIModelId.Value)
                    : null;

                var node = new Node(session)
                {
                    Id = nodeDto.Id,
                    Name = nodeDto.Name,
                    NodeType = Enum.TryParse<NodeType>(nodeDto.NodeType, out var nodeType) ? nodeType : NodeType.Default,
                    PropertiesDictionary = nodeDto.Properties,
                    CreatedAt = nodeDto.CreatedAt,
                    UpdatedAt = nodeDto.UpdatedAt,
                    Status = nodeDto.Status,
                    Parent = parent,
                    Project = project,
                    Template = template,
                    MessageType = nodeDto.MessageType,
                    Level = nodeDto.Level,
                    AIModel = aiModel
                };

                session.Save(node);
                session.CommitTransaction();
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public void UpdateNode(NodeDto nodeDto)
        {
            using var session = new Session();
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
                var aiModel = nodeDto.AIModelId.HasValue
                    ? session.GetObjectByKey<AIModel>(nodeDto.AIModelId.Value)
                    : null;

                node.Name = nodeDto.Name;
                node.NodeType = Enum.TryParse<NodeType>(nodeDto.NodeType, out var nodeType) ? nodeType : NodeType.Default;
                node.PropertiesDictionary = nodeDto.Properties;
                node.UpdatedAt = nodeDto.UpdatedAt;
                node.Status = nodeDto.Status;
                node.Parent = parent;
                node.Project = project;
                node.Template = template;
                node.MessageType = nodeDto.MessageType;
                node.Level = nodeDto.Level;
                node.AIModel = aiModel;

                session.Save(node);
                session.CommitTransaction();
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public void DeleteNode(string id)
        {
            using var session = new Session();
            session.BeginTransaction();

            try
            {
                var node = session.Query<Node>().FirstOrDefault(n => n.Id == id);
                if (node != null)
                {
                    session.Delete(node);
                }
                session.CommitTransaction();
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }
    }
}
