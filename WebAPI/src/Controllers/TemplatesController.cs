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
    public class TemplatesController : ControllerBase
    {
        private readonly NodPTDbContext dbContext;
        private readonly TemplateService _templateService;

        public TemplatesController(NodPTDbContext _dbContext)
        {
            this.dbContext = _dbContext;
            this._templateService = new TemplateService(dbContext);
        }

        [HttpGet]
        public IActionResult GetTemplates() => Ok(_templateService.GetAllTemplates());

        [HttpGet("{id}")]
        public IActionResult GetTemplate(int id)
        {
            var template = _templateService.GetTemplate(id);
            return template == null ? NotFound() : Ok(template);
        }

        [HttpPost]
        public IActionResult CreateTemplate([FromBody] TemplateDto template)
        {
            if (template == null) return BadRequest();

            var createdTemplate = _templateService.CreateTemplate(template);
            return CreatedAtAction(nameof(GetTemplate), new { id = createdTemplate.Id }, createdTemplate);
        }

        [HttpPut("{id}")]
        [CustomAuthorized]
        public IActionResult UpdateTemplate(int id, [FromBody] TemplateDto template)
        {
            if (template == null) return BadRequest();

            var updatedTemplate = _templateService.UpdateTemplate(id, template);
            return updatedTemplate == null ? NotFound() : Ok(updatedTemplate);
        }

        [HttpDelete("{id}")]
        [CustomAuthorized]
        public IActionResult DeleteTemplate(int id)
        {
            var deleted = _templateService.DeleteTemplate(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}