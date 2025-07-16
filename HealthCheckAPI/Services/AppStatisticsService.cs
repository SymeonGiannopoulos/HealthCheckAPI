using HealthCheckAPI.Interface;
using HealthCheckAPI.Models;

namespace HealthCheckAPI.Services
{
    public class AppStatisticsService : IAppStatisticsService
    {
        private readonly IAppStatusLogRepository _logRepository;

        public AppStatisticsService(IAppStatusLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<AppStatisticsDto?> GetStatisticsAsync(string appId)
        {
            var from = DateTime.Today;
            var logs = (await _logRepository.GetLogsByAppIdAsync(appId, from))
                .OrderBy(l => l.CheckedAt)
                .ToList();

            if (!logs.Any())
                return null;

            int totalDowntime = 0;
            int downtimeCount = 0;
            int currentDowntime = 0;
            bool inDowntime = false;

            for (int i = 0; i < logs.Count; i++)
            {
                if (!logs[i].Status)
                {
                    currentDowntime += 1;

                    if (!inDowntime)
                    {
                        downtimeCount++;
                        inDowntime = true;
                    }
                }
                else
                {
                    inDowntime = false;
                }

                totalDowntime += logs[i].Status ? 0 : 1;
            }

            double availabilityPercent = 100.0 * (logs.Count - totalDowntime) / (logs.Count);

            return new AppStatisticsDto
            {
                AppId = appId,
                AverageDowntimeMinutes = downtimeCount > 0 ? (double)totalDowntime / downtimeCount : 0,
                DowntimesToday = downtimeCount,
                TotalDowntimeToday = totalDowntime,
                AvailabilityPercentToday = Math.Round(availabilityPercent, 2)
            };
        }
    }
}
