using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace HealthCheckAPI.Controllers
{
    
        [ApiController]
        [Route("[controller]")]
        public class UserController : ControllerBase
        {
            private readonly IConfiguration _config;

            public UserController(IConfiguration config)
            {
                _config = config;
            }

            [HttpGet]
            public async Task<ActionResult<IEnumerable<UserModel>>> GetUsers()
            {
                var users = new List<UserModel>();
                var connectionString = _config.GetConnectionString("SqliteConnection");

                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Username, Password, Email FROM Users";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new UserModel
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Password = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }

                return Ok(users);
            }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var connectionString = _config.GetConnectionString("SqliteConnection");

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Users WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return NoContent();
        }

    }



}
    


