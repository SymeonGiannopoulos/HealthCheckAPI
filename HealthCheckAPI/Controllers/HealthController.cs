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

        [HttpGet("check-all")]
        public async Task<IActionResult> CheckAll()
        {
            var results = await _healthService.CheckAllInternalAsync();
            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHealth(string id)
        {
            var apps = _config.GetSection("Applications").GetChildren();
            var app = apps.FirstOrDefault(a => a["Id"] == id);
            if (app == null) return NotFound();

            var type = app["Type"];
            var name = app["Name"];

            if (type == "WebApp")
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync(app["HealthCheckUrl"]);

                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(new { Id = id, Name = name, Status = "Healthy" });
                    }
                    else
                    {
                        await _healthService.LogUnhealthyStatusAsync(id, name, "Unhealthy");
                        var userEmails = await _healthService.GetAllUserEmailsAsync();

                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                                email,
                                $"Alert: {name} is Unhealthy",
                                $"The application {name} is unhealthy as of {GetGreekTime()} (Greece time)."
                            );
                        }

                        return Ok(new { Id = id, Name = name, Status = "Unhealthy" });
                    }
                }
                catch (Exception ex)
                {
                    await _healthService.LogUnhealthyStatusAsync(id, name, "Unhealthy");
                    return Ok(new { Id = id, Name = name, Status = "Unhealthy", Message = ex.Message });
                }
            }
            else if (type == "Database")
            {
                try
                {
                    using var connection = new SqlConnection(app["ConnectionString"]);
                    await connection.OpenAsync();

                    using var command = new SqlCommand(app["Query"], connection);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        return Ok(new { Id = id, Name = name, Status = "Healthy" });
                    }
                    else
                    {
                        await _healthService.LogUnhealthyStatusAsync(id, name, "Unhealthy");
                        var userEmails = await _healthService.GetAllUserEmailsAsync();

                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                                email,
                                $"Alert: {name} is Unhealthy",
                                $"The application {name} is unhealthy as of {GetGreekTime()} (Greece time)."
                            );
                        }

                        return Ok(new { Id = id, Name = name, Status = "Unhealthy" });
                    }
                }
                catch (Exception ex)
                {
                    await _healthService.LogUnhealthyStatusAsync(id, name, "Unhealthy");
                    return Ok(new { Id = id, Name = name, Status = "Unhealthy", Message = ex.Message });
                }
            }

            return BadRequest("Unsupported application type");
        }

        private string GetGreekTime()
        {
            var greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            var greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);
            return greeceTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
