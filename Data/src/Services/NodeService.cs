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
                ProjectId = node.Project?.Oid,
                ProjectName = node.Project?.Name,
                TemplateId = node.Template?.Oid,
                TemplateName = node.Template?.Name,
                MessageType = node.MessageType
            };

            // Map MatchingAIModel if available
            if (node.GetMatchingAIModel() != null)
            {
                var aiModel = node.GetMatchingAIModel()!;
                dto.MatchingAIModel = new AIModelDto
                {
                    Id = aiModel.Oid,
                    Name = aiModel.Name,
                    ModelIdentifier = aiModel.ModelIdentifier,
                    MessageType = aiModel.MessageType,
                    NodeType = aiModel.NodeType,
                    Description = aiModel.Description,
                    IsActive = aiModel.IsActive,
                    CreatedAt = aiModel.CreatedAt,
                    UpdatedAt = aiModel.UpdatedAt,
                    TemplateId = aiModel.Template?.Oid,
                    EndpointAddress = aiModel.EndpointAddress,
                    Temperature = aiModel.Temperature,
                    NumPredict = aiModel.NumPredict,
                    TopK = aiModel.TopK,
                    TopP = aiModel.TopP,
                    Seed = aiModel.Seed,
                    NumCtx = aiModel.NumCtx,
                    NumGpu = aiModel.NumGpu,
                    NumThread = aiModel.NumThread,
                    RepeatPenalty = aiModel.RepeatPenalty,
                    Stop = aiModel.Stop
                };
            }

            // Map MatchingPrompts
            dto.MatchingPrompts = node.GetMatchingPrompts().Select(p => new PromptDto
            {
                Id = p.Oid,
                Content = p.Content,
                MessageType = p.MessageType,
                NodeType = p.NodeType,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TemplateId = p.Template?.Oid
            }).ToList();

            return dto;
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

        public void AddNode(NodeDto nodeDto)
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
                    MessageType = nodeDto.MessageType
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
