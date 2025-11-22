using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class ProjectFileService
    {
        private readonly NodPTDbContext context;

        public ProjectFileService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

        public List<ProjectFileDto> GetAllFiles()
        {
            return context.ProjectFiles
                .Include(f => f.Folder)
                .Select(f => new ProjectFileDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Extension = f.Extension,
                    Size = f.Size,
                    MimeType = f.MimeType,
                    Content = f.Content,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    FolderId = f.FolderId,
                    FolderName = f.Folder != null ? f.Folder.Name : null
                }).ToList();
        }

        public ProjectFileDto? GetFile(int id)
        {
            var file = context.ProjectFiles
                .Include(f => f.Folder)
                .FirstOrDefault(f => f.Id == id);
            
            if (file == null) return null;
            
            return new ProjectFileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                Extension = file.Extension,
                Size = file.Size,
                MimeType = file.MimeType,
                Content = file.Content,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt,
                FolderId = file.FolderId,
                FolderName = file.Folder?.Name
            };
        }

        public List<ProjectFileDto> GetFilesByFolder(int folderId)
        {
            return context.ProjectFiles
                .Include(f => f.Folder)
                .Where(f => f.FolderId == folderId)
                .Select(f => new ProjectFileDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Extension = f.Extension,
                    Size = f.Size,
                    MimeType = f.MimeType,
                    Content = f.Content,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    FolderId = f.FolderId,
                    FolderName = f.Folder != null ? f.Folder.Name : null
                }).ToList();
        }

        public ProjectFileDto CreateFile(ProjectFileDto fileDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var file = new ProjectFile
                {
                    Name = fileDto.Name,
                    Path = fileDto.Path,
                    Extension = fileDto.Extension,
                    Size = fileDto.Size,
                    MimeType = fileDto.MimeType,
                    Content = fileDto.Content,
                    FolderId = fileDto.FolderId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.ProjectFiles.Add(file);
                context.SaveChanges();
                transaction.Commit();

                // Reload with includes
                file = context.ProjectFiles
                    .Include(f => f.Folder)
                    .FirstOrDefault(f => f.Id == file.Id);

                fileDto.Id = file!.Id;
                fileDto.CreatedAt = file.CreatedAt;
                fileDto.UpdatedAt = file.UpdatedAt;
                fileDto.FolderName = file.Folder?.Name;

                return fileDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public ProjectFileDto? UpdateFile(int id, ProjectFileDto fileDto)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var file = context.ProjectFiles
                    .Include(f => f.Folder)
                    .FirstOrDefault(f => f.Id == id);

                if (file == null) return null;

                file.Name = fileDto.Name;
                file.Path = fileDto.Path;
                file.Extension = fileDto.Extension;
                file.Size = fileDto.Size;
                file.MimeType = fileDto.MimeType;
                file.Content = fileDto.Content;
                file.FolderId = fileDto.FolderId;
                file.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();
                transaction.Commit();

                fileDto.Id = file.Id;
                fileDto.UpdatedAt = file.UpdatedAt;
                fileDto.FolderName = file.Folder?.Name;

                return fileDto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool DeleteFile(int id)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var file = context.ProjectFiles.FirstOrDefault(f => f.Id == id);

                if (file == null) return false;

                context.ProjectFiles.Remove(file);
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