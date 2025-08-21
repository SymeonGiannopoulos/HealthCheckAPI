using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace HealthCheckAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ApplicationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        
        [HttpGet]
        public IActionResult GetAll()
        {
            var applications = new List<ApplicationConfigModel>();
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Type, HealthCheckUrl, ConnectionString, Query FROM Applications";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                applications.Add(new ApplicationConfigModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    HealthCheckUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ConnectionString = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Query = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return Ok(applications);
        }

        
        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            ApplicationConfigModel app = null;
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Type, HealthCheckUrl, ConnectionString, Query FROM Applications WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                app = new ApplicationConfigModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    HealthCheckUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ConnectionString = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Query = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }

            if (app == null)
                return NotFound();

            return Ok(app);
        }


        [HttpPost]
        public IActionResult Create([FromBody] CreateApplicationRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Application name is required.");

            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Applications (Name, Type, HealthCheckUrl, ConnectionString, Query)
            VALUES (@name, @type, @healthCheckUrl, @connectionString, @query);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            command.Parameters.AddWithValue("@name", request.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@type", request.Type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@healthCheckUrl", request.HealthCheckUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@connectionString", request.ConnectionString ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@query", request.Query ?? (object)DBNull.Value);

            try
            {
                var newId = (int)command.ExecuteScalar();

                var response = new ApplicationConfigModel
                {
                    Id = newId,
                    Name = request.Name,
                    Type = request.Type,
                    HealthCheckUrl = request.HealthCheckUrl,
                    ConnectionString = request.ConnectionString,
                    Query = request.Query
                };

                return CreatedAtAction(nameof(GetById), new { id = newId }, response);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }




        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] ApplicationConfigModel app)
        {
            if (app == null || id != app.Id)
                return BadRequest();

            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Applications
                SET Name = @name, Type = @type, HealthCheckUrl = @healthCheckUrl, ConnectionString = @connectionString, Query = @query
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@name", app.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@type", app.Type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@healthCheckUrl", app.HealthCheckUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@connectionString", app.ConnectionString ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@query", app.Query ?? (object)DBNull.Value);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Applications WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
    }

}


