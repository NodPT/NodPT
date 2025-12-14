using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data.DTOs;
using NodPT.Data.Services;
using DevExpress.Xpo;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        // UnitOfWork is injected for use in future methods that need transaction control
        // Currently, ProjectService creates its own Session instances
        private readonly UnitOfWork unitOfWork;

        public ProjectsController(UnitOfWork _unitOfWork)
        {
            this.unitOfWork = _unitOfWork;
        }

        [HttpGet]
        public IActionResult GetProjects()
        {
            try
            {
                // Service validates user and returns their projects
                var projectService = new ProjectService(unitOfWork, User);
                return Ok(projectService.GetUserProjects());
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProjects");
                return StatusCode(500, new { error = "An error occurred while retrieving projects." });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetProject(int id)
        {
            try
            {
                // Service validates user in constructor - user must own the project
                var projectService = new ProjectService(unitOfWork, User);
                var project = projectService.GetProject(id);
                return project == null ? NotFound() : Ok(project);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProject");
                return StatusCode(500, new { error = "An error occurred while retrieving the project." });
            }
        }

        [HttpPost]
        public IActionResult CreateProject([FromBody] ProjectDto project)
        {
            try
            {
                if (project == null) return BadRequest();
                
                // Service validates user in constructor
                var projectService = new ProjectService(unitOfWork, User);
                var createdProject = projectService.CreateProject(project);
                return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "CreateProject");
                return StatusCode(500, new { error = "An error occurred while creating the project." });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateProject(int id, [FromBody] ProjectDto project)
        {
            try
            {
                if (project == null) return BadRequest();

                var projectService = new ProjectService(unitOfWork);
                var updatedProject = projectService.UpdateProject(id, project);
                return updatedProject == null ? NotFound() : Ok(updatedProject);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "UpdateProject");
                return StatusCode(500, new { error = "An error occurred while updating the project." });
            }
        }

        [HttpPatch("{id}/name")]
        public IActionResult UpdateProjectName(int id, [FromBody] UpdateProjectNameDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { error = "Project name is required." });
                }

                // Service validates user in constructor
                var projectService = new ProjectService(unitOfWork, User);
                var updatedProject = projectService.UpdateProjectName(id, request.Name);
                return updatedProject == null ? NotFound() : Ok(updatedProject);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "UpdateProjectName");
                return StatusCode(500, new { error = "An error occurred while updating the project name." });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProject(int id)
        {
            try
            {
                if (id <= 0) return BadRequest();
                
                // Service validates user in constructor
                var projectService = new ProjectService(unitOfWork, User);
                var deleted = projectService.DeleteProject(id);
                return deleted ? NoContent() : NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "DeleteProject");
                return StatusCode(500, new { error = "An error occurred while deleting the project." });
            }
        }
    }
}