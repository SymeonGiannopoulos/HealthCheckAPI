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
            
            var logs = (await _logRepository.GetLogsByAppIdAsync(appId))
           .OrderBy(l => l.CheckedAt)
           .ToList();


            if (!logs.Any())
                return null;

            int totalDowntime = 0;
            int downtimeCount = 0;
            bool inDowntime = false;

            foreach (var log in logs)
            {
                if (!log.Status)
                {
                    totalDowntime++;

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
            }

            double availabilityPercent = 100.0 * (logs.Count - totalDowntime) / logs.Count;

            return new AppStatisticsDto
            {
                AppId = appId,
                AverageDowntimeMinutes = downtimeCount > 0
         ? Math.Round((double)totalDowntime / downtimeCount, 2)     
         : 0,
                DowntimesCount = downtimeCount,
                TotalDowntime = totalDowntime,
                AvailabilityPercent = Math.Round(availabilityPercent, 2)
            };

        }
    }
}
