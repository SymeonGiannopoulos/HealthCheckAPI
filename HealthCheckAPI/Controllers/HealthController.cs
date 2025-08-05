using HealthCheckAPI.Interface;
using HealthCheckAPI.Models;
using HealthCheckAPI.Notifications;
using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HealthCheckAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Email _emailSender;
        private readonly IHealthMemory _memory;
        private readonly IHealthService _healthService;

        public HealthController(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            Email emailSender,
            IHealthMemory memory,
            IHealthService healthService)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _emailSender = emailSender;
            _memory = memory;
            _healthService = healthService;
        }

        [HttpGet("check-all-health")]
        public async Task<IActionResult> CheckAllHealth()
        {
            var results = await _healthService.CheckAllHealthStatusAsync();
            return Ok(results);
        }

        [HttpGet("check-health/{id}")]
        public async Task<IActionResult> CheckHealthSingleApp(string id)
        {
            var result = await _healthService.CheckSingleAppHealthStatusAsync(id);
            if (result == null) return NotFound($"No application with id '{id}' found.");
            return Ok(result);
        }


    }
}
