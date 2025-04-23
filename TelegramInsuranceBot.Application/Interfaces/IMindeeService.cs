using System.Threading.Tasks;
using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Application.Interfaces
{
    public interface IMindeeService
    {
        Task<MindeeResult> ProcessIdCardAsync(string filePath);
        Task<MindeeResult> ProcessVehicleCardAsync(string filePath);
    }
}



