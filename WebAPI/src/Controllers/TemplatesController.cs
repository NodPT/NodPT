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
    public class TemplatesController : ControllerBase
    {
        private readonly UnitOfWork unitOfWork;
        private readonly TemplateService _templateService;

        public TemplatesController(UnitOfWork _unitOfWork)
        {
            this.unitOfWork = _unitOfWork;
            this._templateService = new TemplateService(unitOfWork);
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