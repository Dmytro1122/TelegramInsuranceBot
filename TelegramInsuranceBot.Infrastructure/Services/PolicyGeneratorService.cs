using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TelegramInsuranceBot.Application.Interfaces;
using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Infrastructure.Services
{
    public class PolicyGeneratorService : IPolicyGeneratorService
    {
        public string GeneratePolicyPdf(MindeeResult passport, MindeeResult vehicle)
        {
            var fileName = $"InsurancePolicy_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine("Documents", "Policies");
            Directory.CreateDirectory(filePath);

            var fullPath = Path.Combine(filePath, fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Content()
                        .Column(column =>
                        {
                            column.Item().Text("Страховий поліс").FontSize(20).Bold().Underline();

                            column.Item().Text($"Ім'я: {passport.FirstName} {passport.LastName}");
                            column.Item().Text($"Номер паспорта: {passport.IdNumber}");

                            column.Item().Text($"Марка авто: {vehicle.VehicleMake}");
                            column.Item().Text($"Модель авто: {vehicle.VehicleModel}");
                            column.Item().Text($"VIN: {vehicle.Vin}");

                            column.Item().PaddingTop(20).Text($"Вартість: 100 USD");
                            column.Item().Text($"Дата: {DateTime.Now:dd.MM.yyyy}");
                        });
                });
            }).GeneratePdf(fullPath);

            return fullPath;
        }
    }
}

