using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class FolderService
    {
        public List<FolderDto> GetAllFolders()
        {
            using var session = new Session();
            var folders = new XPCollection<Folder>(session);
            
            return folders.Select(f => new FolderDto
            {
                Id = f.Oid,
                Name = f.Name,
                Path = f.Path,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                ProjectId = f.Project?.Oid,
                ParentId = f.Parent?.Oid,
                ProjectName = f.Project?.Name,
                ParentName = f.Parent?.Name
            }).ToList();
        }

        public FolderDto? GetFolder(int id)
        {
            using var session = new Session();
            var folder = session.GetObjectByKey<Folder>(id);
            
            if (folder == null) return null;
            
            return new FolderDto
            {
                Id = folder.Oid,
                Name = folder.Name,
                Path = folder.Path,
                CreatedAt = folder.CreatedAt,
                UpdatedAt = folder.UpdatedAt,
                ProjectId = folder.Project?.Oid,
                ParentId = folder.Parent?.Oid,
                ProjectName = folder.Project?.Name,
                ParentName = folder.Parent?.Name
            };
        }

        public List<FolderDto> GetFoldersByProject(int projectId)
        {
            using var session = new Session();
            var project = session.GetObjectByKey<Project>(projectId);
            
            if (project == null) return new List<FolderDto>();
            
            return project.Folders.Select(f => new FolderDto
            {
                Id = f.Oid,
                Name = f.Name,
                Path = f.Path,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                ProjectId = f.Project?.Oid,
                ParentId = f.Parent?.Oid,
                ProjectName = f.Project?.Name,
                ParentName = f.Parent?.Name
            }).ToList();
        }

        public FolderDto CreateFolder(FolderDto folderDto)
        {
            using var session = new Session();
            
            var project = folderDto.ProjectId.HasValue 
                ? session.GetObjectByKey<Project>(folderDto.ProjectId.Value) 
                : null;
            var parent = folderDto.ParentId.HasValue 
                ? session.GetObjectByKey<Folder>(folderDto.ParentId.Value) 
                : null;
            
            var folder = new Folder(session)
            {
                Name = folderDto.Name,
                Path = folderDto.Path,
                Project = project,
                Parent = parent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            session.Save(folder);
            session.CommitTransaction();
            
            folderDto.Id = folder.Oid;
            folderDto.CreatedAt = folder.CreatedAt;
            folderDto.UpdatedAt = folder.UpdatedAt;
            folderDto.ProjectName = folder.Project?.Name;
            folderDto.ParentName = folder.Parent?.Name;
            
            return folderDto;
        }

        public FolderDto? UpdateFolder(int id, FolderDto folderDto)
        {
            using var session = new Session();
            var folder = session.GetObjectByKey<Folder>(id);
            
            if (folder == null) return null;
            
            var project = folderDto.ProjectId.HasValue 
                ? session.GetObjectByKey<Project>(folderDto.ProjectId.Value) 
                : null;
            var parent = folderDto.ParentId.HasValue 
                ? session.GetObjectByKey<Folder>(folderDto.ParentId.Value) 
                : null;
            
            folder.Name = folderDto.Name;
            folder.Path = folderDto.Path;
            folder.Project = project;
            folder.Parent = parent;
            folder.UpdatedAt = DateTime.UtcNow;
            
            session.Save(folder);
            session.CommitTransaction();
            
            folderDto.Id = folder.Oid;
            folderDto.UpdatedAt = folder.UpdatedAt;
            folderDto.ProjectName = folder.Project?.Name;
            folderDto.ParentName = folder.Parent?.Name;
            
            return folderDto;
        }

        public bool DeleteFolder(int id)
        {
            using var session = new Session();
            var folder = session.GetObjectByKey<Folder>(id);
            
            if (folder == null) return false;
            
            session.Delete(folder);
            session.CommitTransaction();
            
            return true;
        }
    }
}