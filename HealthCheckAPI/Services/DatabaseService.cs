using Dapper;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Data.SqlClient;

namespace HealthCheckAPI.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
        }

        
        public async Task<int> CountErrorsAsync(string identifier)
        {
            string query = "SELECT COUNT(*) FROM ErrorLogs WHERE Name = @identifier OR AppId = @identifier";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@identifier", identifier);

            return (int)await command.ExecuteScalarAsync();
        }

        
        public async Task<int> CountErrorsTodayAsync(string identifier)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM ErrorLogs 
                WHERE (Name = @identifier OR AppId = @identifier) 
                AND CAST(Timestamp AS DATE) = CAST(GETDATE() AS DATE)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@identifier", identifier);

            return (int)await command.ExecuteScalarAsync();
        }

        
        public async Task<int> CountErrorsOnDateAsync(string identifier, DateTime date)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM ErrorLogs 
                WHERE (Name = @identifier OR AppId = @identifier)
                AND CAST(Timestamp AS DATE) = @date";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@identifier", identifier);
            command.Parameters.AddWithValue("@date", date.Date);

            return (int)await command.ExecuteScalarAsync();
        }

        
        public async Task<DateTime?> GetLastFailureDateAsync(string identifier)
        {
            string query = @"
                SELECT TOP 1 Timestamp 
                FROM ErrorLogs 
                WHERE Name = @identifier OR AppId = @identifier
                ORDER BY Timestamp DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@identifier", identifier);

            var result = await command.ExecuteScalarAsync();
            return result != null ? (DateTime?)Convert.ToDateTime(result) : null;
        }
        public async Task<List<ApplicationModel>> GetAllApplicationsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Applications";
            var result = await connection.QueryAsync<ApplicationModel>(query);
            return result.ToList();
        }
    }
}
