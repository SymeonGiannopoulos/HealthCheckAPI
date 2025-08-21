using HealthCheckAPI.Interfaces;
using System.Data.SqlClient;
using HealthCheckAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AuditLogService : IAuditLogService
{
    private readonly string _connectionString;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _dbconnectionString;

    public AuditLogService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConnection");
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string actionType, string? entityType = null, string? entityId = null, string? details = null, string? userId = null, string? ipAddress = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        DateTime timestamp;
        try
        {
            var greeceTZ = TimeZoneInfo.FindSystemTimeZoneById("Eastern European Standard Time");
            timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTZ);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                var greeceTZ = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");
                timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTZ);
            }
            catch
            {
                timestamp = DateTime.UtcNow;
            }
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(@"
            INSERT INTO AuditLog (UserId, Timestamp, ActionType, EntityType, EntityId, Details, IpAddress)
            VALUES (@UserId, @Timestamp, @ActionType, @EntityType, @EntityId, @Details, @IpAddress);", connection);

        command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", timestamp);
        command.Parameters.AddWithValue("@ActionType", actionType);
        command.Parameters.AddWithValue("@EntityType", (object?)entityType ?? DBNull.Value);
        command.Parameters.AddWithValue("@EntityId", (object?)entityId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Details", (object?)details ?? DBNull.Value);
        command.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAuditLogAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = new SqlCommand("DELETE FROM AuditLog WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        int rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    public async Task DeleteAuditLogsAsync(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Φτιάχνουμε μια παράμετρο για κάθε id
        var parameters = string.Join(",", ids.Select((id, index) => $"@id{index}"));

        using var command = new SqlCommand($"DELETE FROM AuditLog WHERE Id IN ({parameters})", connection);

        for (int i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", ids[i]);
        }

        await command.ExecuteNonQueryAsync();
    }




    public async Task<List<AuditLog>> GetAuditLogsAsync(int limit = 10)
    {
        var logs = new List<AuditLog>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(@"
            SELECT TOP (@Limit) Id, UserId, Timestamp, ActionType, EntityType, EntityId, Details, IpAddress
            FROM AuditLog
            ORDER BY Timestamp DESC;", connection);

        command.Parameters.AddWithValue("@Limit", limit);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLog
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? null : reader.GetString(1),
                Timestamp = reader.GetDateTime(2),
                ActionType = reader.GetString(3),
                EntityType = reader.IsDBNull(4) ? null : reader.GetString(4),
                EntityId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Details = reader.IsDBNull(6) ? null : reader.GetString(6),
                IpAddress = reader.IsDBNull(7) ? null : reader.GetString(7),
            });
        }

        return logs;
    }
}
