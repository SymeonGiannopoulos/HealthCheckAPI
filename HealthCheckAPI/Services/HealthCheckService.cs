using HealthCheckAPI.Controllers;

namespace HealthCheckAPI.Services
{
    public class HealthCheckService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public HealthCheckService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var controller = scope.ServiceProvider.GetRequiredService<HealthController>();
                    await controller.CheckAllInternalAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
