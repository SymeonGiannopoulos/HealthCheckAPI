using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        [HttpGet("logs/app/{appId}")]
        public async Task<IActionResult> GetErrorLogsByAppId(string appId)
        {
            var errorLogs = new List<object>();
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT * FROM ErrorLogs WHERE AppId = @appId ORDER BY Timestamp DESC", connection);
            command.Parameters.AddWithValue("@appId", appId);

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

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllErrors()
        {
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(@"
        DELETE FROM HealthStatusLog;
        DELETE FROM ErrorLogs;
    ", connection);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            return Ok(new { message = "All errors cleared.", rowsAffected });
        }



    }
}
