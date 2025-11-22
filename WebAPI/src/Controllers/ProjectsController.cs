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
    public class ProjectsController : ControllerBase
    {
        // NodPTDbContext is injected for use in future methods that need transaction control
        // Currently, ProjectService creates its own DbContext instances
        private readonly NodPTDbContext dbContext;

        public ProjectsController(NodPTDbContext _dbContext)
        {
            this.dbContext = _dbContext;
        }

        [HttpGet]
        [CustomAuthorized("Admin")]
        public IActionResult GetProjects()
        {
            try
            {
                var projectService = new ProjectService(dbContext);
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
                var projectService = new ProjectService(dbContext);
                var project = projectService.GetProject(id);
                return project == null ? NotFound() : Ok(project);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProject");
                return StatusCode(500, new { error = "An error occurred while retrieving the project." });
            }
        }

        [HttpGet("me")]
        public IActionResult GetMyProjects()
        {
            try
            {
                // Get user from token
                var user = UserService.GetUser(User, dbContext);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or invalid" });
                }

                var projectService = new ProjectService(dbContext);
                var projects = projectService.GetProjectsByUser(user.FirebaseUid);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetMyProjects");
                return StatusCode(500, new { error = "An error occurred while retrieving user projects." });
            }
        }

        [HttpPost]
        public IActionResult CreateProject([FromBody] ProjectDto project)
        {
            try
            {
                if (project == null) return BadRequest();
                
                // Get user from token
                var user = UserService.GetUser(User, dbContext);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or invalid" });
                }

                var projectService = new ProjectService(dbContext);
                var createdProject = projectService.CreateProject(project, user.FirebaseUid);
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

                var projectService = new ProjectService(dbContext);
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

                // Get user from token
                var user = UserService.GetUser(User, dbContext);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or invalid" });
                }

                var projectService = new ProjectService(dbContext);
                var updatedProject = projectService.UpdateProjectName(id, request.Name, user.FirebaseUid);
                return updatedProject == null ? NotFound() : Ok(updatedProject);
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
                
                // Get user from token
                var user = UserService.GetUser(User, dbContext);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found or invalid" });
                }

                var projectService = new ProjectService(dbContext);
                var deleted = projectService.DeleteProject(id, user.FirebaseUid);
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