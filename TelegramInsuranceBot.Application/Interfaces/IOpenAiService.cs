using System.Threading.Tasks;

namespace TelegramInsuranceBot.Application.Interfaces
{
    public interface IOpenAiService
    {
        Task<string> AskAsync(string prompt);
    }
}
