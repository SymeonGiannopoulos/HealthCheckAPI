namespace HealthCheckAPI.Models
{
    public class AppStatisticsModel
    {
        public string Name { get; set; }
        public int DowntimeCount { get; set; }        
        public TimeSpan TotalDowntimeDuration { get; set; } 
        public TimeSpan MeanDowntimeDuration { get; set; }  
        public double AvailabilityPercentage { get; set; }  
    }
}
