namespace HealthCheckAPI.Models
{
    public class AppStatisticsDto
    {
        public string AppId { get; set; } = string.Empty;
        public double AverageDowntimeMinutes { get; set; }
        public int DowntimesCount { get; set; }
        public int TotalDowntime { get; set; }
        public double AvailabilityPercent { get; set; }
    }
}
