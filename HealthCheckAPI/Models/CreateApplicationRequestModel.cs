namespace HealthCheckAPI.Models
{
    public class CreateApplicationRequestModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string HealthCheckUrl { get; set; }
        public string ConnectionString { get; set; }
        public string Query { get; set; }
    }
}
