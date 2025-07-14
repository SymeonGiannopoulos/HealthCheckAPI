using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HealthCheckAPI.Controllers
{
    [Authorize]
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
            var errors = new List<ErrorModel>();
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SELECT Id, Name, Status, Timestamp FROM HealthStatusLog", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        errors.Add(new ErrorModel
                        {
                            Id = reader["Id"].ToString(),
                            Name = reader["Name"].ToString(),
                            Status = reader["Status"].ToString(),
                            Timestamp = reader["Timestamp"].ToString()
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
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT * FROM ErrorLogs ORDER BY Timestamp DESC", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                errorLogs.Add(new
                {
                    Id = reader["Id"].ToString(),
                    AppId = reader["AppId"].ToString(),
                    Name = reader["Name"].ToString(),
                    Status = reader["Status"].ToString(),
                    Timestamp = reader["Timestamp"].ToString()
                });
            }

            return Ok(errorLogs);
        }

        [HttpGet("logs/id/{id}")]
        public async Task<IActionResult> GetErrorLogsById(string id)
        {
            var errorLogs = new List<object>();
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT * FROM ErrorLogs WHERE Id = @id ORDER BY Timestamp DESC", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                errorLogs.Add(new
                {
                    Id = reader["Id"].ToString(),
                    AppId = reader["AppId"].ToString(),
                    Name = reader["Name"].ToString(),
                    Status = reader["Status"].ToString(),
                    Timestamp = reader["Timestamp"].ToString()
                });
            }

            return Ok(errorLogs);
        }

    }
}
