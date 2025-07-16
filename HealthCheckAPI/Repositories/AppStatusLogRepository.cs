using Dapper;
using System.Data.SqlClient;
using HealthCheckAPI.Models;
using HealthCheckAPI.Interface;

namespace HealthCheckAPI.Repositories
{
    public class AppStatusLogRepository : IAppStatusLogRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AppStatusLogRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("SqlServerConnection");
        }

        public async Task<IEnumerable<AppStatusLogModel>> GetLogsByAppIdAsync(string appId, DateTime from)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"SELECT * FROM AppStatusLog 
                          WHERE AppId = @AppId AND CheckedAt >= @From";
            var logs = await connection.QueryAsync<AppStatusLogModel>(query, new { AppId = appId, From = from });
            return logs.AsList();
        }

        public async Task AddLogAsync(AppStatusLogModel log)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = @"INSERT INTO AppStatusLog (AppId, Status, CheckedAt)
                          VALUES (@AppId, @Status, @CheckedAt)";
            await connection.ExecuteAsync(query, log);
        }
    }
}
