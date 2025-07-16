namespace HealthCheckAPI.Interfaces
{
    public interface IRetryService
    {
        Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int delayMilliseconds = 1000);
    }
}
