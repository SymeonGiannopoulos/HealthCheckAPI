namespace HealthCheckAPI.Models
{
    public class AppStatusLogModel
    {
        public int Id { get; set; }
        public string AppId { get; set; } = string.Empty;
        public bool Status { get; set; } 
        public DateTime CheckedAt { get; set; }
    }
}
