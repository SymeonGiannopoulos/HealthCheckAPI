namespace HealthCheckAPI.Interfaces
{
    public interface IHealthMemory
    {
        Dictionary<string, string> StatusMap { get; }
    }

}
