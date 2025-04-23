using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using TelegramInsuranceBot.Application.Interfaces;
using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Infrastructure.Services
{
    public class InsurancePolicyGenerator : IInsurancePolicyGenerator
    {
        public string GeneratePolicyPdf(MindeeResult idCard, MindeeResult vehicleCard)
        {
            var fileName = $"Policy_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine("Documents", fileName);

            Directory.CreateDirectory("Documents");

            var policyText = $"""
            🛡 Страховий поліс

            Поліс №: {Guid.NewGuid():N}
            Страхувальник: {idCard.FirstName} {idCard.LastName}
            Ідентифікаційний номер: {idCard.IdNumber}

            Автомобіль: {vehicleCard.VehicleMake} {vehicleCard.VehicleModel}
            VIN-код: {vehicleCard.Vin}

            Дата видачі: {DateTime.Now:dd.MM.yyyy}
            Строк дії: 1 рік
            Сума страхування: 100 USD
            """;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.Content()
                        .Text(policyText)
                        .FontSize(14)
                        .LineHeight(1.5f);
                });
            }).GeneratePdf(filePath);

            return filePath;
        }
    }
}
