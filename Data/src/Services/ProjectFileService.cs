using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class ProjectFileService
    {
        public List<ProjectFileDto> GetAllFiles()
        {
            using var session = new Session();
            var files = new XPCollection<ProjectFile>(session);
            
            return files.Select(f => new ProjectFileDto
            {
                Id = f.Oid,
                Name = f.Name,
                Path = f.Path,
                Extension = f.Extension,
                Size = f.Size,
                MimeType = f.MimeType,
                Content = f.Content,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                FolderId = f.Folder?.Oid,
                FolderName = f.Folder?.Name
            }).ToList();
        }

        public ProjectFileDto? GetFile(int id)
        {
            using var session = new Session();
            var file = session.GetObjectByKey<ProjectFile>(id);
            
            if (file == null) return null;
            
            return new ProjectFileDto
            {
                Id = file.Oid,
                Name = file.Name,
                Path = file.Path,
                Extension = file.Extension,
                Size = file.Size,
                MimeType = file.MimeType,
                Content = file.Content,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt,
                FolderId = file.Folder?.Oid,
                FolderName = file.Folder?.Name
            };
        }

        public List<ProjectFileDto> GetFilesByFolder(int folderId)
        {
            using var session = new Session();
            var folder = session.GetObjectByKey<Folder>(folderId);
            
            if (folder == null) return new List<ProjectFileDto>();
            
            return folder.Files.Select(f => new ProjectFileDto
            {
                Id = f.Oid,
                Name = f.Name,
                Path = f.Path,
                Extension = f.Extension,
                Size = f.Size,
                MimeType = f.MimeType,
                Content = f.Content,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                FolderId = f.Folder?.Oid,
                FolderName = f.Folder?.Name
            }).ToList();
        }

        public ProjectFileDto CreateFile(ProjectFileDto fileDto)
        {
            using var session = new Session();
            
            var folder = fileDto.FolderId.HasValue 
                ? session.GetObjectByKey<Folder>(fileDto.FolderId.Value) 
                : null;
            
            var file = new ProjectFile(session)
            {
                Name = fileDto.Name,
                Path = fileDto.Path,
                Extension = fileDto.Extension,
                Size = fileDto.Size,
                MimeType = fileDto.MimeType,
                Content = fileDto.Content,
                Folder = folder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            session.Save(file);
            session.CommitTransaction();
            
            fileDto.Id = file.Oid;
            fileDto.CreatedAt = file.CreatedAt;
            fileDto.UpdatedAt = file.UpdatedAt;
            fileDto.FolderName = file.Folder?.Name;
            
            return fileDto;
        }

        public ProjectFileDto? UpdateFile(int id, ProjectFileDto fileDto)
        {
            using var session = new Session();
            var file = session.GetObjectByKey<ProjectFile>(id);
            
            if (file == null) return null;
            
            var folder = fileDto.FolderId.HasValue 
                ? session.GetObjectByKey<Folder>(fileDto.FolderId.Value) 
                : null;
            
            file.Name = fileDto.Name;
            file.Path = fileDto.Path;
            file.Extension = fileDto.Extension;
            file.Size = fileDto.Size;
            file.MimeType = fileDto.MimeType;
            file.Content = fileDto.Content;
            file.Folder = folder;
            file.UpdatedAt = DateTime.UtcNow;
            
            session.Save(file);
            session.CommitTransaction();
            
            fileDto.Id = file.Oid;
            fileDto.UpdatedAt = file.UpdatedAt;
            fileDto.FolderName = file.Folder?.Name;
            
            return fileDto;
        }

        public bool DeleteFile(int id)
        {
            using var session = new Session();
            var file = session.GetObjectByKey<ProjectFile>(id);
            
            if (file == null) return false;
            
            session.Delete(file);
            session.CommitTransaction();
            
            return true;
        }
    }
}