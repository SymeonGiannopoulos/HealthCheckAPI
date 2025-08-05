using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HealthCheckAPI.Interfaces;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        await _next(context);

      
        var path = context.Request.Path.ToString().ToLower();

        if (path.StartsWith("/api/auditlog"))
        {
            return;
        }

        var userId = context.User.Identity?.Name ?? "Anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode.ToString();

        string actionType = $"{method} {path}";

        await auditLogService.LogAsync(
            actionType: actionType,
            entityType: "HTTP Request",
            entityId: null,
            details: $"Status: {statusCode}",
            userId: userId,
            ipAddress: ipAddress
        );
    }
}
