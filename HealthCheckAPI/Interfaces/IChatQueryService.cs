using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public interface IChatQueryService
    {
        Task<string> AnswerQuestionAsync(string question);
    }
}
