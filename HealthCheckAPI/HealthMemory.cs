using HealthCheckAPI.Interfaces;

namespace HealthCheckAPI
{
    public class HealthMemory : IHealthMemory
    {
        public Dictionary<string, string> StatusMap { get; } = new();
    }
}
