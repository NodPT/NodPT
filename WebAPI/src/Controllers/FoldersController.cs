using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data;
using NodPT.Data.DTOs;
using NodPT.Data.Services;

namespace NodPT.API.Controllers
{
    [CustomAuthorized("Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly NodPTDbContext dbContext;
        private readonly FolderService _folderService;

        public FoldersController(NodPTDbContext _dbContext)
        {
            this.dbContext = _dbContext;
            this._folderService = new FolderService(dbContext);
        }

        [CustomAuthorized("Admin")]
        [HttpGet]
        public IActionResult GetFolders() => Ok(_folderService.GetAllFolders());

        [HttpGet("{id}")]
        public IActionResult GetFolder(int id)
        {
            var folder = _folderService.GetFolder(id);
            return folder == null ? NotFound() : Ok(folder);
        }

        [HttpGet("project/{projectId}")]
        public IActionResult GetFoldersByProject(int projectId)
        {
            var folders = _folderService.GetFoldersByProject(projectId);
            return Ok(folders);
        }

        [HttpPost]
        public IActionResult CreateFolder([FromBody] FolderDto folder)
        {
            if (folder == null) return BadRequest();

            var createdFolder = _folderService.CreateFolder(folder);
            return CreatedAtAction(nameof(GetFolder), new { id = createdFolder.Id }, createdFolder);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateFolder(int id, [FromBody] FolderDto folder)
        {
            if (folder == null) return BadRequest();

            var updatedFolder = _folderService.UpdateFolder(id, folder);
            return updatedFolder == null ? NotFound() : Ok(updatedFolder);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteFolder(int id)
        {
            var deleted = _folderService.DeleteFolder(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}