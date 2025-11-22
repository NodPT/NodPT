using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class NodeService
    {
        private readonly NodPTDbContext context;

        public NodeService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

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
                ParentId = node.ParentId,
                ProjectId = node.ProjectId,
                ProjectName = node.Project?.Name,
                TemplateId = node.TemplateId,
                TemplateName = node.Template?.Name,
                MessageType = node.MessageType,
                Level = node.Level,
                AIModelId = node.AIModelId,
                AIModelName = node.AIModel?.Name
            };

            // Map MatchingAIModel if available
            if (node.MatchingAIModel != null)
            {
                dto.MatchingAIModel = new AIModelDto
                {
                    Id = node.MatchingAIModel.Id,
                    Name = node.MatchingAIModel.Name,
                    ModelIdentifier = node.MatchingAIModel.ModelIdentifier,
                    MessageType = node.MatchingAIModel.MessageType,
                    Level = node.MatchingAIModel.Level,
                    Description = node.MatchingAIModel.Description,
                    IsActive = node.MatchingAIModel.IsActive,
                    CreatedAt = node.MatchingAIModel.CreatedAt,
                    UpdatedAt = node.MatchingAIModel.UpdatedAt,
                    TemplateId = node.MatchingAIModel.TemplateId
                };
            }

            // Map MatchingPrompts
            dto.MatchingPrompts = node.MatchingPrompts.Select(p => new PromptDto
            {
                Id = p.Id,
                Content = p.Content,
                MessageType = p.MessageType,
                Level = p.Level,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TemplateId = p.TemplateId
            }).ToList();

            return dto;
        }

        public List<NodeDto> GetAllNodes()
        {
            return context.Nodes
                .Include(n => n.Project)
                .Include(n => n.Template)
                .Include(n => n.AIModel)
                .Include(n => n.Parent)
                .Select(n => MapToDto(n))
                .ToList();
        }

        public NodeDto? GetNode(string id)
        {
            var node = context.Nodes
                .Include(n => n.Project)
                .Include(n => n.Template)
                .Include(n => n.AIModel)
                .Include(n => n.Parent)
                .FirstOrDefault(n => n.Id == id);
            return node == null ? null : MapToDto(node);
        }

        public List<NodeDto> GetNodesByProject(int projectId)
        {
            return context.Nodes
                .Include(n => n.Project)
                .Include(n => n.Template)
                .Include(n => n.AIModel)
                .Include(n => n.Parent)
                .Where(n => n.ProjectId == projectId)
                .Select(n => MapToDto(n))
                .ToList();
        }

        public void AddNode(NodeDto nodeDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var node = new Node
                {
                    Id = nodeDto.Id,
                    Name = nodeDto.Name,
                    NodeType = Enum.TryParse<NodeType>(nodeDto.NodeType, out var nodeType) ? nodeType : NodeType.Default,
                    PropertiesDictionary = nodeDto.Properties,
                    CreatedAt = nodeDto.CreatedAt,
                    UpdatedAt = nodeDto.UpdatedAt,
                    Status = nodeDto.Status,
                    ParentId = nodeDto.ParentId,
                    ProjectId = nodeDto.ProjectId,
                    TemplateId = nodeDto.TemplateId,
                    MessageType = nodeDto.MessageType,
                    Level = nodeDto.Level,
                    AIModelId = nodeDto.AIModelId
                };

                context.Nodes.Add(node);
                context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateNode(NodeDto nodeDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var node = context.Nodes.FirstOrDefault(n => n.Id == nodeDto.Id);
                if (node == null) return;

                node.Name = nodeDto.Name;
                node.NodeType = Enum.TryParse<NodeType>(nodeDto.NodeType, out var nodeType) ? nodeType : NodeType.Default;
                node.PropertiesDictionary = nodeDto.Properties;
                node.UpdatedAt = nodeDto.UpdatedAt;
                node.Status = nodeDto.Status;
                node.ParentId = nodeDto.ParentId;
                node.ProjectId = nodeDto.ProjectId;
                node.TemplateId = nodeDto.TemplateId;
                node.MessageType = nodeDto.MessageType;
                node.Level = nodeDto.Level;
                node.AIModelId = nodeDto.AIModelId;

                context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteNode(string id)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var node = context.Nodes.FirstOrDefault(n => n.Id == id);
                if (node != null)
                {
                    context.Nodes.Remove(node);
                    context.SaveChanges();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
