using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class TemplateService
    {
        public List<TemplateDto> GetAllTemplates()
        {
            using var session = new Session();
            var templates = new XPCollection<Template>(session);
            
            return templates.Select(t => new TemplateDto
            {
                Id = t.Oid,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category,
                Version = t.Version,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();
        }

        public TemplateDto? GetTemplate(int id)
        {
            using var session = new Session();
            var template = session.GetObjectByKey<Template>(id);
            
            if (template == null) return null;
            
            return new TemplateDto
            {
                Id = template.Oid,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Version = template.Version,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        public TemplateDto CreateTemplate(TemplateDto templateDto)
        {
            using var session = new Session();
            session.BeginTransaction();
            
            try
            {
                var template = new Template(session)
                {
                    Name = templateDto.Name,
                    Description = templateDto.Description,
                    Category = templateDto.Category,
                    Version = templateDto.Version,
                    IsActive = templateDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                session.Save(template);
                session.CommitTransaction();
                
                templateDto.Id = template.Oid;
                templateDto.CreatedAt = template.CreatedAt;
                templateDto.UpdatedAt = template.UpdatedAt;
                
                return templateDto;
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public TemplateDto? UpdateTemplate(int id, TemplateDto templateDto)
        {
            using var session = new Session();
            session.BeginTransaction();
            
            try
            {
                var template = session.GetObjectByKey<Template>(id);
                
                if (template == null) return null;
                
                template.Name = templateDto.Name;
                template.Description = templateDto.Description;
                template.Category = templateDto.Category;
                template.Version = templateDto.Version;
                template.IsActive = templateDto.IsActive;
                template.UpdatedAt = DateTime.UtcNow;
                
                session.Save(template);
                session.CommitTransaction();
                
                templateDto.Id = template.Oid;
                templateDto.UpdatedAt = template.UpdatedAt;
                
                return templateDto;
            }
            catch
            {
                session.RollbackTransaction();
                throw;
            }
        }

        public bool DeleteTemplate(int id)
        {
            using var session = new Session();
            session.BeginTransaction();
            
            try
            {
                var template = session.GetObjectByKey<Template>(id);
                
                if (template == null) return false;
                
                session.Delete(template);
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