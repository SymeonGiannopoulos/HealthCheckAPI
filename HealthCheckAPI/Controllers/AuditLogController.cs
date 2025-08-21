using HealthCheckAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 100)
        {
            var logs = await _auditLogService.GetAuditLogsAsync(limit);
            return Ok(logs);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditLog(int id)
        {
            var deleted = await _auditLogService.DeleteAuditLogAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [HttpPost("delete-many")]
        public async Task<IActionResult> DeleteManyAuditLogs([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest("No ids provided.");

            await _auditLogService.DeleteAuditLogsAsync(ids);

            return NoContent();
        }


    }
}
