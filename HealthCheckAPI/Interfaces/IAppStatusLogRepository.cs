using HealthCheckAPI.Models;

public interface IAppStatusLogRepository
{
    Task<IEnumerable<AppStatusLogModel>> GetLogsByAppIdAsync(string appId); 
    Task<IEnumerable<AppStatusLogModel>> GetLogsByAppIdFromDateAsync(string appId, DateTime from); 

    Task AddLogAsync(AppStatusLogModel log);
}
