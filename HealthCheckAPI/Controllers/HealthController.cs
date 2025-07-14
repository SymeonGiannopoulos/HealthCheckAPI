using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System;
using HealthCheckAPI.Notifications;
using System.Data;
using System.Data.SqlClient; 
using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Authorization;
using HealthCheckAPI.Interface;
using HealthCheckAPI.Services;

namespace HealthCheckAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Email _emailSender;
        private readonly IHealthMemory _memory;
        private readonly IHealthService _healthService;

        public HealthController(IConfiguration config, IHttpClientFactory httpClientFactory, Email emailSender, IHealthMemory memory, IHealthService healthService)
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
            var results = await CheckAllInternalAsync();
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
                        $"The application {name} is unhealthy as of {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time")):dd/MM/yyyy HH:mm:ss} (Greece time)."
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
                    // Changed to SQL Server from SQLite
                    using var connection = new SqlConnection(app["ConnectionString"]); // Changed to SQL Server
                    await connection.OpenAsync();

                    using var command = new SqlCommand(app["Query"], connection); // Changed to SQL Server
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
                        $"The application {name} is unhealthy as of {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time")):dd/MM/yyyy HH:mm:ss} (Greece time)."
);
                        }
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("check")]
        public async Task<List<object>> CheckAllInternalAsync()
        {
            var applications = _config.GetSection("Applications").Get<List<ApplicationConfigModel>>();
            var results = new List<object>();

            var greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            var greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);

            foreach (var app in applications)
            {
                string status = "Healthy";

                if (app.Type == "WebApp")
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        var response = await client.GetAsync(app.HealthCheckUrl);
                        if (!response.IsSuccessStatusCode)
                            status = "Unhealthy";
                    }
                    catch
                    {
                        status = "Unhealthy";
                    }
                }
                else if (app.Type == "Database")
                {
                    try
                    {
                        // Changed to SQL Server from SQLite
                        using var connection = new SqlConnection(app.ConnectionString); 
                        await connection.OpenAsync();
                        using var command = connection.CreateCommand();
                        command.CommandText = app.Query;
                        await command.ExecuteScalarAsync();
                    }
                    catch
                    {
                        status = "Unhealthy";
                    }
                }

                if (status == "Healthy")
                {
                    _memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

                    if (previousStatus != "Healthy")
                    {
                        var connectionString = _config.GetConnectionString("SqlServerConnection"); 
                        using var connection = new SqlConnection(connectionString); 
                        await connection.OpenAsync();

                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM HealthStatusLog WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", app.Id);
                        await command.ExecuteNonQueryAsync();

                        var userEmails = await _healthService.GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                            email,
                            $"Update: {app.Name} is Healthy",
                            $"The application {app.Name} is healthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time).");
                        }
                    }

                }
                else
                {
                    _memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

                    if (previousStatus != "Unhealthy")
                    {
                        await _healthService.LogUnhealthyStatusAsync(app.Id, app.Name, status);

                        var userEmails = await _healthService.GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                        email,
                        $"Alert: {app.Name} is Unealthy",
                        $"The application {app.Name} is unhealthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time).");
                        }
                    }
                }

                _memory.StatusMap[app.Id] = status;

                results.Add(new
                {
                    Id = app.Id,
                    Name = app.Name,
                    Status = status
                });
            }

            return results;
        }
    }
}
