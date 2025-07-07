using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using HealthCheckAPI.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = HealthCheckAPI.Models.Error;

namespace HealthCheckAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ErrorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetErrors()
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
    }


}

