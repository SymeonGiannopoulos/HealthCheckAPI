using HealthCheckAPI.Interface;
using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppStatisticsController : ControllerBase
    {
        private readonly IAppStatisticsService _statisticsService;

        public AppStatisticsController(IAppStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStatistics(string id)
        {
            var stats = await _statisticsService.GetStatisticsAsync(id);

            if (stats == null)
                return NotFound($"No statistics found for app with ID '{id}'.");

            return Ok(stats);
        }
    }
}
