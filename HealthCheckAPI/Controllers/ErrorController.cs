using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HealthCheckAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ErrorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        
        [HttpGet("live")]
        public IActionResult GetLiveErrors()
        {
            var errors = new List<Error>();
            var connectionString = _configuration.GetConnectionString("SqliteConnection");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, Status, Timestamp FROM HealthStatusLog";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        errors.Add(new Error
                        {
                            Id = reader.GetString(0),
                            Name = reader.GetString(1),
                            Status = reader.GetString(2),
                            Timestamp = reader.GetString(3)
                        });
                    }
                }
            }

            return Ok(errors);
        }

        
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllErrorLogs()
        {
            var errorLogs = new List<object>();
            var connectionString = _configuration.GetConnectionString("SqliteConnection");

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ErrorLogs ORDER BY Timestamp DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                errorLogs.Add(new
                {
                    Id = reader["Id"],
                    AppId = reader["AppId"],
                    Name = reader["Name"],
                    Status = reader["Status"],
                    Timestamp = reader["Timestamp"]
                });
            }

            return Ok(errorLogs);
        }

        
        [HttpGet("logs/{name}")]
        public async Task<IActionResult> GetErrorLogsByName(string name)
        {
            var errorLogs = new List<object>();
            var connectionString = _configuration.GetConnectionString("SqliteConnection");

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ErrorLogs WHERE Name = @name ORDER BY Timestamp DESC";
            command.Parameters.AddWithValue("@name", name);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                errorLogs.Add(new
                {
                    Id = reader["Id"],
                    AppId = reader["AppId"],
                    Name = reader["Name"],
                    Status = reader["Status"],
                    Timestamp = reader["Timestamp"]
                });
            }

            return Ok(errorLogs);
        }
    }
}
