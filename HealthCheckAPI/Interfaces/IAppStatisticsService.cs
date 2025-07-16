using HealthCheckAPI.Models;

public interface IAppStatisticsService
{
    Task<AppStatisticsDto?> GetStatisticsAsync(string appId);
}
