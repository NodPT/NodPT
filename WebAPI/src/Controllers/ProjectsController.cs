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
        [CustomAuthorized("Admin")]
        public IActionResult GetProjects()
        {
            try
            {
                var projectService = new ProjectService(unitOfWork);
                return Ok(projectService.GetAllProjects());
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
                var projectService = new ProjectService(unitOfWork);
                var project = projectService.GetProject(id);
                return project == null ? NotFound() : Ok(project);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProject");
                return StatusCode(500, new { error = "An error occurred while retrieving the project." });
            }
        }

        [HttpGet("user/{firebaseUid}")]
        public IActionResult GetProjectsByUser(string firebaseUid)
        {
            try
            {
                var projectService = new ProjectService(unitOfWork);
                var projects = projectService.GetProjectsByUser(firebaseUid);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProjectsByUser");
                return StatusCode(500, new { error = "An error occurred while retrieving user projects." });
            }
        }

        [HttpPost]
        public IActionResult CreateProject([FromBody] ProjectDto project)
        {
            try
            {
                if (project == null) return BadRequest();
                string? firebaseUid = UserService.GetFirebaseUIDFromContent(User);
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }
                var projectService = new ProjectService(unitOfWork);
                var createdProject = projectService.CreateProject(project, firebaseUid);
                return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
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

        [HttpDelete("{id}")]
        public IActionResult DeleteProject(int id)
        {
            try
            {
                if (id <= 0) return BadRequest();
                string? firebaseUid = UserService.GetFirebaseUIDFromContent(User);
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }
                var projectService = new ProjectService(unitOfWork);
                var deleted = projectService.DeleteProject(id, firebaseUid);
                return deleted ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "DeleteProject");
                return StatusCode(500, new { error = "An error occurred while deleting the project." });
            }
        }
    }
}