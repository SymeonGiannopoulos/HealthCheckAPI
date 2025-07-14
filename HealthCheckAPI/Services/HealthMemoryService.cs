using HealthCheckAPI.Models;
using HealthCheckAPI.Interface;

namespace HealthCheckAPI.Services
{
    public class HealthMemoryService : IHealthMemory
    {
        public Dictionary<string, string> StatusMap { get; } = new();
    }
}
