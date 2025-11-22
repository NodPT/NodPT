using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data;
using NodPT.Data.DTOs;
using NodPT.Data.Services;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectFilesController : ControllerBase
    {
        private readonly NodPTDbContext dbContext;
        private readonly ProjectFileService _fileService;

        public ProjectFilesController(NodPTDbContext _dbContext)
        {
            this.dbContext = _dbContext;
            this._fileService = new ProjectFileService(dbContext);
        }

        [HttpGet]
        public IActionResult GetFiles() => Ok(_fileService.GetAllFiles());

        [HttpGet("{id}")]
        public IActionResult GetFile(int id)
        {
            var file = _fileService.GetFile(id);
            return file == null ? NotFound() : Ok(file);
        }

        [HttpGet("folder/{folderId}")]
        public IActionResult GetFilesByFolder(int folderId)
        {
            var files = _fileService.GetFilesByFolder(folderId);
            return Ok(files);
        }

        [HttpPost]
        public IActionResult CreateFile([FromBody] ProjectFileDto file)
        {
            if (file == null) return BadRequest();

            var createdFile = _fileService.CreateFile(file);
            return CreatedAtAction(nameof(GetFile), new { id = createdFile.Id }, createdFile);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateFile(int id, [FromBody] ProjectFileDto file)
        {
            if (file == null) return BadRequest();

            var updatedFile = _fileService.UpdateFile(id, file);
            return updatedFile == null ? NotFound() : Ok(updatedFile);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteFile(int id)
        {
            var deleted = _fileService.DeleteFile(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}