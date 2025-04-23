using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Application.Interfaces
{
    public interface IInsurancePolicyGenerator
    {
        string GeneratePolicyPdf(MindeeResult idCard, MindeeResult vehicleCard);
    }
}

