using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class TemplateService
    {
        private readonly NodPTDbContext context;

        public TemplateService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

        public List<TemplateDto> GetAllTemplates()
        {
            return context.Templates
                .Select(t => new TemplateDto
                {
                    Id = t.Id,
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
            var template = context.Templates.FirstOrDefault(t => t.Id == id);

            if (template == null) return null;

            return new TemplateDto
            {
                Id = template.Id,
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
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var template = new Template
                {
                    Name = templateDto.Name,
                    Description = templateDto.Description,
                    Category = templateDto.Category,
                    Version = templateDto.Version,
                    IsActive = templateDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Templates.Add(template);
                context.SaveChanges();
                transaction.Commit();

                templateDto.Id = template.Id;
                templateDto.CreatedAt = template.CreatedAt;
                templateDto.UpdatedAt = template.UpdatedAt;

                return templateDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public TemplateDto? UpdateTemplate(int id, TemplateDto templateDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var template = context.Templates.FirstOrDefault(t => t.Id == id);

                if (template == null) return null;

                template.Name = templateDto.Name;
                template.Description = templateDto.Description;
                template.Category = templateDto.Category;
                template.Version = templateDto.Version;
                template.IsActive = templateDto.IsActive;
                template.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();
                transaction.Commit();

                templateDto.Id = template.Id;
                templateDto.UpdatedAt = template.UpdatedAt;

                return templateDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool DeleteTemplate(int id)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var template = context.Templates.FirstOrDefault(t => t.Id == id);

                if (template == null) return false;

                context.Templates.Remove(template);
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