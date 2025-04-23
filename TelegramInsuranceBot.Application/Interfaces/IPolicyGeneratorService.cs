using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Application.Interfaces
{
    public interface IPolicyGeneratorService
    {
        string GeneratePolicyPdf(MindeeResult idCardData, MindeeResult vehicleCardData);
    }
}

