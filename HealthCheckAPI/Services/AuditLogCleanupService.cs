using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

public class AuditLogCleanupService : BackgroundService
{
    private readonly ILogger<AuditLogCleanupService> _logger;
    private readonly string _connectionString;
    private readonly int _retentionDays;

    public AuditLogCleanupService(ILogger<AuditLogCleanupService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("SqlServerConnection");
        _retentionDays = configuration.GetValue<int>("AuditLog:RetentionDays", 30); 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync();
                _logger.LogInformation("Audit log cleanup completed at {time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log cleanup");
            }

            
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task CleanupOldLogsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("DELETE FROM AuditLog WHERE Timestamp < @CutoffDate", connection);
        command.Parameters.AddWithValue("@CutoffDate", cutoffDate);

        var rowsDeleted = await command.ExecuteNonQueryAsync();
        _logger.LogInformation("Deleted {count} audit log entries older than {cutoff}", rowsDeleted, cutoffDate);
    }
}
