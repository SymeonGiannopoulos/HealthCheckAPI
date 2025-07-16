using HealthCheckAPI.Interfaces;

namespace HealthCheckAPI.Services
{
    public class RetryService : IRetryService
    {
        private readonly ILogger<RetryService> _logger;

        public RetryService(ILogger<RetryService> logger)
        {
            _logger = logger;
        }

        public async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int delayMilliseconds = 1000)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Attempt {attempt} failed: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        _logger.LogError($"Operation failed after {maxRetries} retries.");
                        throw; 
                    }

                    await Task.Delay(delayMilliseconds);
                }
            }

            return default;
        }
    }

}
