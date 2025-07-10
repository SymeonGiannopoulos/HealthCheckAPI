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
using HealthCheckAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;

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

        //    private readonly Timer _timer;
        //    private readonly Dictionary<string, string> _previousStatuses = new();



        public HealthController(IConfiguration config, IHttpClientFactory httpClientFactory, Email emailSender, IHealthMemory memory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _emailSender = emailSender;
            _memory = memory;


            /*           _timer = new Timer(async _ =>
                       {
                           await CheckAllInternalAsync();
                       }, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));*/

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
                        await LogUnhealthyStatusAsync(id, name, "Unhealthy");

                        var userEmails = await GetAllUserEmailsAsync();
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
                    await LogUnhealthyStatusAsync(id, name, "Unhealthy");
                    return Ok(new { Id = id, Name = name, Status = "Unhealthy", Message = ex.Message });
                }
            }
            else if (type == "Database")
            {
                try
                {
                    using var connection = new SqliteConnection(app["ConnectionString"]);
                    await connection.OpenAsync();

                    using var command = new SqliteCommand(app["Query"], connection);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        return Ok(new { Id = id, Name = name, Status = "Healthy" });
                    }
                    else
                    {
                        await LogUnhealthyStatusAsync(id, name, "Unhealthy");

                        var userEmails = await GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                        "simosgiann@gmail.com",
                        $"Alert: {name} is Unhealthy",
                        $"The application {name} is unhealthy as of {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time")):dd/MM/yyyy HH:mm:ss} (Greece time)."
);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await LogUnhealthyStatusAsync(id, name, "Unhealthy");
                    return Ok(new { Id = id, Name = name, Status = "Unhealthy", Message = ex.Message });
                }
            }

            return BadRequest("Unsupported application type");
        }

        public async Task LogUnhealthyStatusAsync(string id, string name, string status)
        {
            var connectionString = _config.GetConnectionString("SqliteConnection");

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            TimeZoneInfo greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            DateTime greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);
            string timestamp = greeceTime.ToString("yyyy-MM-dd HH:mm:ss");

            using (var command1 = connection.CreateCommand())
            {
                command1.CommandText = @"
            INSERT INTO HealthStatusLog (Id, Name, Status, Timestamp)
            VALUES ($id, $name, $status, $timestamp)";
                command1.Parameters.AddWithValue("$id", id);
                command1.Parameters.AddWithValue("$name", name);
                command1.Parameters.AddWithValue("$status", status);
                command1.Parameters.AddWithValue("$timestamp", timestamp);
                await command1.ExecuteNonQueryAsync();
            }

            using (var command2 = connection.CreateCommand())
            {
                command2.CommandText = @"
            INSERT INTO ErrorLogs (AppId, Name, Status, Timestamp)
            VALUES (@appId, @name, @status, @timestamp)";
                command2.Parameters.AddWithValue("@appId", id);
                command2.Parameters.AddWithValue("@name", name);
                command2.Parameters.AddWithValue("@status", status);
                command2.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                await command2.ExecuteNonQueryAsync();
            }
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("check")]
        public async Task<List<object>> CheckAllInternalAsync()
        {
            var applications = _config.GetSection("Applications").Get<List<ApplicationConfig>>();
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
                        using var connection = new SqliteConnection(app.ConnectionString);
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
                    //_previousStatuses.TryGetValue(app.Id, out var previousStatus);
                    _memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

                    if (previousStatus != "Healthy")
                    {
                        var connectionString = _config.GetConnectionString("SqliteConnection");
                        using var connection = new SqliteConnection(connectionString);
                        await connection.OpenAsync();

                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM HealthStatusLog WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", app.Id);
                        await command.ExecuteNonQueryAsync();

                        var userEmails = await GetAllUserEmailsAsync();
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
                    //_previousStatuses.TryGetValue(app.Id, out var previousStatus);
                    _memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

                    if (previousStatus != "Unhealthy")
                    {
                        await LogUnhealthyStatusAsync(app.Id, app.Name, status);

                        var userEmails = await GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            _emailSender.SendEmail(
                        email,
                        $"Alert: {app.Name} is Unealthy",
                        $"The application {app.Name} is unhealthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time).");
                        }
                    }
                }


                //_previousStatuses[app.Id] = status;
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
        public async Task<List<string>> GetAllUserEmailsAsync()
        {
            var emails = new List<string>();
            var connectionString = _config.GetConnectionString("SqliteConnection");

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Email FROM Users WHERE Email IS NOT NULL";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var email = reader.GetString(0);
                emails.Add(email);
            }

            return emails;
        }






    }
}