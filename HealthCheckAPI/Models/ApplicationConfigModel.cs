namespace HealthCheckAPI.Models
{
    public class ApplicationConfigModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "WebApp" ή "Database"

        // Για WebApp
        public string HealthCheckUrl { get; set; }

        // Για Database
        public string ConnectionString { get; set; }
        public string Query { get; set; }
    }
}
