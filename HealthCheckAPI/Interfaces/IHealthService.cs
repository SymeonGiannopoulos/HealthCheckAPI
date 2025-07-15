using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthCheckAPI.Interface
{
    public interface IHealthService
    {
        Task<object> CheckSingleAppAsync(string id);

        Task<List<object>> CheckAllInternalAsync();
        Task<List<string>> GetAllUserEmailsAsync();
        Task LogUnhealthyStatusAsync(string id, string name, string status);
    }
}
