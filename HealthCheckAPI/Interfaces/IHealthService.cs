using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public interface IHealthService
    {
        Task<List<string>> GetAllUserEmailsAsync();
        Task LogUnhealthyStatusAsync(string id, string name, string status);
    }
}
