using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;  // Αλλαγή εδώ
using System.Data;

namespace HealthCheckAPI.Services
{
    public class HealthService : IHealthService
    {
        private readonly IConfiguration _config;

        public HealthService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<string>> GetAllUserEmailsAsync()
        {
            var emails = new List<string>();
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
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

        public async Task LogUnhealthyStatusAsync(string id, string name, string status)
        {
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            TimeZoneInfo greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            DateTime greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);
         
            DateTime timestamp = greeceTime;

            using (var command1 = connection.CreateCommand())
            {
                command1.CommandText = @"
                    INSERT INTO HealthStatusLog (Id, Name, Status, Timestamp)
                    VALUES (@id, @name, @status, @timestamp)";
                command1.Parameters.AddWithValue("@id", id);
                command1.Parameters.AddWithValue("@name", name);
                command1.Parameters.AddWithValue("@status", status);
                command1.Parameters.AddWithValue("@timestamp", timestamp);
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
                command2.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                await command2.ExecuteNonQueryAsync();
            }
        }
    }
}
