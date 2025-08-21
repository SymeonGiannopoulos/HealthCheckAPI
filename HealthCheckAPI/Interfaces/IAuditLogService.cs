using HealthCheckAPI.Models;

namespace HealthCheckAPI.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string actionType, string? entityType = null, string? entityId = null, string? details = null, string? userId = null, string? ipAddress = null);

        Task<List<AuditLog>> GetAuditLogsAsync(int limit = 100);

        Task<bool> DeleteAuditLogAsync(int id);
        Task DeleteAuditLogsAsync(List<int> ids);
    }
}
