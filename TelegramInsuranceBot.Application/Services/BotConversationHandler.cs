using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using TelegramInsuranceBot.Application.Interfaces;
using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Application.Services
{
    public class BotConversationHandler : IBotConversationHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly IOpenAiService _openAiService;
        private readonly IDocumentStorageService _storageService;
        private readonly IMindeeService _mindeeService;
        private readonly IPolicyGeneratorService _policyGenerator;

        private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

        public BotConversationHandler(
            TelegramBotClient botClient,
            IOpenAiService openAiService,
            IDocumentStorageService storageService,
            IMindeeService mindeeService,
            IPolicyGeneratorService policyGenerator)
        {
            _botClient = botClient;
            _openAiService = openAiService;
            _storageService = storageService;
            _mindeeService = mindeeService;
            _policyGenerator = policyGenerator;
        }

        public async Task StartAsync()
        {
            _botClient.OnMessage += OnMessageReceived;
            _botClient.StartReceiving();

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"🤖 Bot started. Username: {me.Username}");
        }

        private async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message?.From == null) return;

            var userId = e.Message.From.Id;
            var message = e.Message.Text?.Trim().ToLower() ?? "";
            var session = _sessions.GetOrAdd(userId, new UserSession());

            // ✅ Генерація PDF полісу одразу після погодження
            if (session.IsAwaitingPaymentConfirmation && (message == "так" || message == "yes"))
            {
                session.IsAwaitingPaymentConfirmation = false;

                var pdfPath = _policyGenerator.GeneratePolicyPdf(session.IdCardData, session.VehicleCardData);

                await using var stream = File.OpenRead(pdfPath);
                await _botClient.SendDocumentAsync(userId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, "InsurancePolicy.pdf"));

                await _botClient.SendTextMessageAsync(userId, "✅ Ваш страховий поліс готовий! Дякуємо за звернення. Якщо потрібна ще допомога — напишіть /start");
                return;
            }

            // ❌ Відмова від оплати
            if (session.IsAwaitingPaymentConfirmation && (message == "ні" || message == "no"))
            {
                session.IsAwaitingPaymentConfirmation = false;
                await _botClient.SendTextMessageAsync(userId, "😔 Розумію. Якщо передумаєте — просто надішліть фото паспорта ще раз.");
                return;
            }

            // 🔁 Підтвердження правильності документів
            if (session.IsAwaitingConfirmation)
            {
                if (message == "так")
                {
                    session.IsAwaitingConfirmation = false;

                    await _botClient.SendTextMessageAsync(userId, "💰 Вартість страхування: 100 USD\nЧи погоджуєтесь ви? (Так / Ні)");
                    session.IsAwaitingPaymentConfirmation = true;
                    return;
                }

                if (message == "ні")
                {
                    session.PassportPath = null;
                    session.VehiclePath = null;
                    session.IsAwaitingConfirmation = false;

                    await _botClient.SendTextMessageAsync(userId, "🔄 Будь ласка, надішліть фото паспорта ще раз.");
                    return;
                }
            }

            if (message == "/start" || message.Contains("привіт"))
            {
                session.PassportPath = null;
                session.VehiclePath = null;
                session.IsAwaitingConfirmation = false;
                session.IsAwaitingPaymentConfirmation = false;

                await _botClient.SendTextMessageAsync(userId, "👋 Привіт! Надішліть фото вашого паспорта.");
                return;
            }

            if (e.Message.Type == MessageType.Photo && e.Message.Photo?.Any() == true)
            {
                var bestPhoto = e.Message.Photo.Last();

                if (string.IsNullOrEmpty(session.PassportPath))
                {
                    var path = await _storageService.SavePhotoAsync(bestPhoto.FileId, "passport", userId);
                    session.PassportPath = path;

                    await _botClient.SendTextMessageAsync(userId, "✅ Паспорт отримано. Тепер надішліть фото техпаспорта.");
                    return;
                }

                if (string.IsNullOrEmpty(session.VehiclePath))
                {
                    var path = await _storageService.SavePhotoAsync(bestPhoto.FileId, "vehicle", userId);
                    session.VehiclePath = path;

                    await _botClient.SendTextMessageAsync(userId, "✅ Техпаспорт отримано. 🔍 Опрацьовую документи...");

                    try
                    {
                        var passportData = await _mindeeService.ProcessIdCardAsync(session.PassportPath);
                        var vehicleData = await _mindeeService.ProcessVehicleCardAsync(session.VehiclePath);

                        session.IdCardData = passportData;
                        session.VehicleCardData = vehicleData;

                        var passportText = $"📄 Паспорт:\nІм'я: {passportData?.FirstName ?? "-"} {passportData?.LastName ?? "-"}";
                        var vehicleText = $"📄 Техпаспорт:\nМарка: {vehicleData?.VehicleMake ?? "-"}\nМодель: {vehicleData?.VehicleModel ?? "-"}";

                        await _botClient.SendTextMessageAsync(userId, passportText);
                        await _botClient.SendTextMessageAsync(userId, vehicleText);
                        await _botClient.SendTextMessageAsync(userId, "Все правильно? (Так / Ні)");

                        session.IsAwaitingConfirmation = true;
                    }
                    catch (Exception ex)
                    {
                        await _botClient.SendTextMessageAsync(userId, $"❌ Помилка при обробці документів: {ex.Message}");
                    }

                    return;
                }

                await _botClient.SendTextMessageAsync(userId, "📌 Ви вже надіслали обидва документи.");
                return;
            }

            await _botClient.SendTextMessageAsync(userId, "📸 Надішліть, будь ласка, фото паспорта або техпаспорта.");
        }
    }
}
