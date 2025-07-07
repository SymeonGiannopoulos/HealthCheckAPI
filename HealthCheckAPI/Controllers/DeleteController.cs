using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

[ApiController]
[Route("api/[controller]")]
public class DeleteController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DeleteController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpDelete("clear-errors")]
    public async Task<IActionResult> ClearErrors()
    {
        var connectionString = _configuration.GetConnectionString("SqliteConnection");

        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM HealthStatusLog";

            await command.ExecuteNonQueryAsync();
        }

        return NoContent();
    }
}
