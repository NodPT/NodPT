using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class ProjectService
    {
        private UnitOfWork session;

        public ProjectService(UnitOfWork unitOfWork)
        {
            this.session = unitOfWork;
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

        private List<NodeDto> GetProjectNodes(Project project)
        {
            return project.Nodes.Select(n => MapNodeToDto(n)).ToList();
        }

        public List<ProjectDto> GetAllProjects()
        {

            var projects = new XPCollection<Project>(session);

            return projects.Select(p => new ProjectDto
            {
                Id = p.Oid,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UserId = p.User?.Oid,
                TemplateId = p.Template?.Oid,
                UserEmail = p.User?.Email,
                TemplateName = p.Template?.Name,
                Nodes = GetProjectNodes(p)
            }).ToList();
        }

        public ProjectDto? GetProject(int id)
        {

            var project = session.GetObjectByKey<Project>(id);

            if (project == null) return null;

            return new ProjectDto
            {
                Id = project.Oid,
                Name = project.Name,
                Description = project.Description,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                UserId = project.User?.Oid,
                TemplateId = project.Template?.Oid,
                UserEmail = project.User?.Email,
                TemplateName = project.Template?.Name,
                Nodes = GetProjectNodes(project)
            };
        }

        public List<ProjectDto> GetProjectsByUser(string firebaseUid)
        {
            var user = UserService.GetUser(firebaseUid, this.session);// session.Query<User>().FirstOrDefault(x=>x.FirebaseUid == userId);

            if (user == null) return new List<ProjectDto>();

            return user.Projects.Select(p => new ProjectDto
            {
                Id = p.Oid,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UserId = p.User?.Oid,
                TemplateId = p.Template?.Oid,
                UserEmail = p.User?.Email,
                TemplateName = p.Template?.Name,
                Nodes = GetProjectNodes(p)
            }).ToList();
        }

        public ProjectDto CreateProject(ProjectDto projectDto, string firebaseUid)
        {
            try
            {
                if (session == null)
                {
                    throw new ArgumentNullException(nameof(session), "Session cannot be null");
                }

                var user = UserService.GetUser(firebaseUid, session);
                if (user == null)
                {
                    throw new ArgumentException("Invalid Firebase UID", nameof(firebaseUid));
                }

                if (projectDto.TemplateId == null)
                {
                    throw new ArgumentException("Template ID cannot be null", nameof(projectDto.TemplateId));
                }


                session.BeginTransaction();
                var template = session.Query<Template>().FirstOrDefault(t => t.Oid == projectDto.TemplateId);

                if (template == null)
                {
                    throw new ArgumentException("Invalid Template ID", nameof(projectDto.TemplateId));
                }

                var project = new Project(session)
                {
                    Name = projectDto.Name,
                    Description = projectDto.Description,
                    IsActive = true,
                    User = user,
                    Template = template,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                session.Save(project);

                // Check if a default Director-level node exists for this project
                var existingDirectorNode = project.Nodes.FirstOrDefault(n => n.Level == LevelEnum.Director);

                if (existingDirectorNode == null)
                {
                    // Create a default Director-level node
                    var defaultNode = new Node(session)
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Director",
                        NodeType = NodeType.Default,
                        Level = LevelEnum.Director,
                        MessageType = MessageTypeEnum.Discussion,
                        Project = project,
                        Template = template,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    };

                    session.Save(defaultNode);

                    // Add a first chat message to the Director node
                    var firstMessage = new ChatMessage(session)
                    {
                        Sender = "ai",
                        Message = "Hello, how can I help you today?",
                        Timestamp = DateTime.UtcNow,
                        Node = defaultNode,
                        User = user,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    };

                    session.Save(firstMessage);
                }

                session.CommitTransaction();

                projectDto.Id = project.Oid;
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
                session.RollbackTransaction();
                throw;
            }
        }

        public ProjectDto? UpdateProject(int id, ProjectDto projectDto)
        {

            session.BeginTransaction();

            try
            {
                var project = session.GetObjectByKey<Project>(id);

                if (project == null) return null;

                var user = projectDto.UserId.HasValue
                    ? session.GetObjectByKey<User>(projectDto.UserId.Value)
                    : null;
                var template = projectDto.TemplateId.HasValue
                    ? session.GetObjectByKey<Template>(projectDto.TemplateId.Value)
                    : null;

                project.Name = projectDto.Name;
                project.Description = projectDto.Description;
                project.IsActive = projectDto.IsActive;
                project.User = user;
                project.Template = template;
                project.UpdatedAt = DateTime.UtcNow;

                session.Save(project);
                session.CommitTransaction();

                projectDto.Id = project.Oid;
                projectDto.UpdatedAt = project.UpdatedAt;
                projectDto.UserEmail = project.User?.Email;
                projectDto.TemplateName = project.Template?.Name;
                projectDto.Nodes = GetProjectNodes(project);

                return projectDto;
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public bool DeleteProject(int id, string firebaseUid)
        {

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session), "Session cannot be null");
            }

            var user = UserService.GetUser(firebaseUid, session);
            if (user == null)
            {
                throw new ArgumentException("Invalid Firebase UID", nameof(firebaseUid));
            }

            session.BeginTransaction();

            try
            {
                var project = session.Query<Project>()
                    .FirstOrDefault(p => p.Oid == id && p.User != null && p.User.Oid == user.Oid);

                if (project == null)
                {
                    throw new ArgumentException("Project not found or user unauthorized", nameof(id));
                }

                session.Delete(project);
                session.CommitTransaction();

                return true;
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }
    }
}