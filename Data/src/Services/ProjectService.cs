using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class ProjectService
    {
        private NodPTDbContext context;

        public ProjectService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

        private NodeDto MapNodeToDto(Node node)
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

        private List<NodeDto> GetProjectNodes(Project project)
        {
            return project.Nodes.Select(n => MapNodeToDto(n)).ToList();
        }

        public List<ProjectDto> GetAllProjects()
        {
            return context.Projects
                .Include(p => p.User)
                .Include(p => p.Template)
                .Include(p => p.Nodes)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UserId = p.UserId,
                    TemplateId = p.TemplateId,
                    UserEmail = p.User != null ? p.User.Email : null,
                    TemplateName = p.Template != null ? p.Template.Name : null,
                    Nodes = GetProjectNodes(p)
                }).ToList();
        }

        public ProjectDto? GetProject(int id)
        {
            var project = context.Projects
                .Include(p => p.User)
                .Include(p => p.Template)
                .Include(p => p.Nodes)
                .FirstOrDefault(p => p.Id == id);

            if (project == null) return null;

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                UserId = project.UserId,
                TemplateId = project.TemplateId,
                UserEmail = project.User?.Email,
                TemplateName = project.Template?.Name,
                Nodes = GetProjectNodes(project)
            };
        }

        public List<ProjectDto> GetProjectsByUser(string firebaseUid)
        {
            var user = UserService.GetUser(firebaseUid, this.context);

            if (user == null) return new List<ProjectDto>();

            return context.Projects
                .Include(p => p.User)
                .Include(p => p.Template)
                .Include(p => p.Nodes)
                .Where(p => p.UserId == user.Id)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UserId = p.UserId,
                    TemplateId = p.TemplateId,
                    UserEmail = p.User != null ? p.User.Email : null,
                    TemplateName = p.Template != null ? p.Template.Name : null,
                    Nodes = GetProjectNodes(p)
                }).ToList();
        }

        public ProjectDto CreateProject(ProjectDto projectDto, string firebaseUid)
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context), "Context cannot be null");
                }

                var user = UserService.GetUser(firebaseUid, context);
                if (user == null)
                {
                    throw new ArgumentException("Invalid Firebase UID", nameof(firebaseUid));
                }

                if (projectDto.TemplateId == null)
                {
                    throw new ArgumentException("Template ID cannot be null", nameof(projectDto.TemplateId));
                }

                var template = context.Templates.FirstOrDefault(t => t.Id == projectDto.TemplateId);

                if (template == null)
                {
                    throw new ArgumentException("Invalid Template ID", nameof(projectDto.TemplateId));
                }

                var project = new Project
                {
                    Name = projectDto.Name,
                    Description = projectDto.Description,
                    IsActive = true,
                    UserId = user.Id,
                    TemplateId = template.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Projects.Add(project);
                context.SaveChanges();

                // Check if a default Director-level node exists for this project
                var existingDirectorNode = context.Nodes.FirstOrDefault(n => n.ProjectId == project.Id && n.Level == LevelEnum.Director);

                if (existingDirectorNode == null)
                {
                    // Create a default Director-level node
                    var defaultNode = new Node
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Director",
                        NodeType = NodeType.Default,
                        Level = LevelEnum.Director,
                        MessageType = MessageTypeEnum.Discussion,
                        ProjectId = project.Id,
                        TemplateId = template.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    };

                    context.Nodes.Add(defaultNode);
                    context.SaveChanges();

                    // Add a first chat message to the Director node
                    var firstMessage = new ChatMessage
                    {
                        Sender = "ai",
                        Message = "Hello, how can I help you today?",
                        Timestamp = DateTime.UtcNow,
                        NodeId = defaultNode.Id,
                        UserId = user.Id,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    };

                    context.ChatMessages.Add(firstMessage);
                    context.SaveChanges();
                }

                transaction.Commit();

                // Reload project with includes
                project = context.Projects
                    .Include(p => p.User)
                    .Include(p => p.Template)
                    .Include(p => p.Nodes)
                    .FirstOrDefault(p => p.Id == project.Id);

                projectDto.Id = project!.Id;
                projectDto.CreatedAt = project.CreatedAt;
                projectDto.UpdatedAt = project.UpdatedAt;
                projectDto.UserEmail = project.User?.Email;
                projectDto.TemplateName = project.Template?.Name;
                projectDto.Nodes = GetProjectNodes(project);
                projectDto.IsActive = true;
                return projectDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public ProjectDto? UpdateProject(int id, ProjectDto projectDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var project = context.Projects
                    .Include(p => p.User)
                    .Include(p => p.Template)
                    .Include(p => p.Nodes)
                    .FirstOrDefault(p => p.Id == id);

                if (project == null) return null;

                User? user = null;
                Template? template = null;

                if (projectDto.UserId.HasValue)
                {
                    user = context.Users.FirstOrDefault(u => u.Id == projectDto.UserId.Value);
                }
                if (projectDto.TemplateId.HasValue)
                {
                    template = context.Templates.FirstOrDefault(t => t.Id == projectDto.TemplateId.Value);
                }

                project.Name = projectDto.Name;
                project.Description = projectDto.Description;
                project.IsActive = projectDto.IsActive;
                project.UserId = user?.Id;
                project.TemplateId = template?.Id;
                project.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();
                transaction.Commit();

                projectDto.Id = project.Id;
                projectDto.UpdatedAt = project.UpdatedAt;
                projectDto.UserEmail = project.User?.Email;
                projectDto.TemplateName = project.Template?.Name;
                projectDto.Nodes = GetProjectNodes(project);

                return projectDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public ProjectDto? UpdateProjectName(int id, string name, string firebaseUid)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "Context cannot be null");
            }

            var user = UserService.GetUser(firebaseUid, context);
            if (user == null)
            {
                throw new ArgumentException("Invalid Firebase UID", nameof(firebaseUid));
            }

            using var transaction = context.Database.BeginTransaction();

            try
            {
                var project = context.Projects
                    .Include(p => p.User)
                    .Include(p => p.Template)
                    .Include(p => p.Nodes)
                    .FirstOrDefault(p => p.Id == id && p.UserId == user.Id);

                if (project == null) return null;

                project.Name = name;
                project.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();
                transaction.Commit();

                return new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    IsActive = project.IsActive,
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt,
                    UserId = project.UserId,
                    TemplateId = project.TemplateId,
                    UserEmail = project.User?.Email,
                    TemplateName = project.Template?.Name,
                    Nodes = GetProjectNodes(project)
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool DeleteProject(int id, string firebaseUid)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "Context cannot be null");
            }

            var user = UserService.GetUser(firebaseUid, context);
            if (user == null)
            {
                throw new ArgumentException("Invalid Firebase UID", nameof(firebaseUid));
            }

            using var transaction = context.Database.BeginTransaction();

            try
            {
                var project = context.Projects
                    .FirstOrDefault(p => p.Id == id && p.UserId == user.Id);

                if (project == null)
                {
                    throw new ArgumentException("Project not found or user unauthorized", nameof(id));
                }

                context.Projects.Remove(project);
                context.SaveChanges();
                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}