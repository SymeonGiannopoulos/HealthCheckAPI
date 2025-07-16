namespace HealthCheckAPI.Models
{
    public class AppStatisticsDto
    {
        public string AppId { get; set; } = string.Empty;
        public double AverageDowntimeMinutes { get; set; }
        public int DowntimesToday { get; set; }
        public int TotalDowntimeToday { get; set; }
        public double AvailabilityPercentToday { get; set; }
    }
}
