using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data.Services;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly LogService _logService;

        public LogsController(LogService logService)
        {
            this._logService = logService;
        }

        [HttpGet]
        public IActionResult GetLogs()
        {
            try
            {
                var logs = _logService.GetAllLogs();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                // If we can't retrieve logs, return the error directly
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
