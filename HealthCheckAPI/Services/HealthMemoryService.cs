using HealthCheckAPI.Models;
using HealthCheckAPI.Interface;

namespace HealthCheckAPI
{
    public class HealthMemory : IHealthMemory
    {
        public Dictionary<string, string> StatusMap { get; } = new();
    }
}
