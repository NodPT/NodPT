using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class FolderService
    {
        private readonly NodPTDbContext context;

        public FolderService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

        public List<FolderDto> GetAllFolders()
        {
            return context.Folders
                .Include(f => f.Project)
                .Include(f => f.Parent)
                .Select(f => new FolderDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    ProjectId = f.ProjectId,
                    ParentId = f.ParentId,
                    ProjectName = f.Project != null ? f.Project.Name : null,
                    ParentName = f.Parent != null ? f.Parent.Name : null
                }).ToList();
        }

        public FolderDto? GetFolder(int id)
        {
            var folder = context.Folders
                .Include(f => f.Project)
                .Include(f => f.Parent)
                .FirstOrDefault(f => f.Id == id);
            
            if (folder == null) return null;
            
            return new FolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                Path = folder.Path,
                CreatedAt = folder.CreatedAt,
                UpdatedAt = folder.UpdatedAt,
                ProjectId = folder.ProjectId,
                ParentId = folder.ParentId,
                ProjectName = folder.Project?.Name,
                ParentName = folder.Parent?.Name
            };
        }

        public List<FolderDto> GetFoldersByProject(int projectId)
        {
            return context.Folders
                .Include(f => f.Project)
                .Include(f => f.Parent)
                .Where(f => f.ProjectId == projectId)
                .Select(f => new FolderDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    ProjectId = f.ProjectId,
                    ParentId = f.ParentId,
                    ProjectName = f.Project != null ? f.Project.Name : null,
                    ParentName = f.Parent != null ? f.Parent.Name : null
                }).ToList();
        }

        public FolderDto CreateFolder(FolderDto folderDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var folder = new Folder
                {
                    Name = folderDto.Name,
                    Path = folderDto.Path,
                    ProjectId = folderDto.ProjectId,
                    ParentId = folderDto.ParentId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Folders.Add(folder);
                context.SaveChanges();
                transaction.Commit();

                // Reload with includes
                folder = context.Folders
                    .Include(f => f.Project)
                    .Include(f => f.Parent)
                    .FirstOrDefault(f => f.Id == folder.Id);

                folderDto.Id = folder!.Id;
                folderDto.CreatedAt = folder.CreatedAt;
                folderDto.UpdatedAt = folder.UpdatedAt;
                folderDto.ProjectName = folder.Project?.Name;
                folderDto.ParentName = folder.Parent?.Name;

                return folderDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public FolderDto? UpdateFolder(int id, FolderDto folderDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var folder = context.Folders
                    .Include(f => f.Project)
                    .Include(f => f.Parent)
                    .FirstOrDefault(f => f.Id == id);

                if (folder == null) return null;

                folder.Name = folderDto.Name;
                folder.Path = folderDto.Path;
                folder.ProjectId = folderDto.ProjectId;
                folder.ParentId = folderDto.ParentId;
                folder.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();
                transaction.Commit();

                folderDto.Id = folder.Id;
                folderDto.UpdatedAt = folder.UpdatedAt;
                folderDto.ProjectName = folder.Project?.Name;
                folderDto.ParentName = folder.Parent?.Name;

                return folderDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool DeleteFolder(int id)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var folder = context.Folders.FirstOrDefault(f => f.Id == id);

                if (folder == null) return false;

                context.Folders.Remove(folder);
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