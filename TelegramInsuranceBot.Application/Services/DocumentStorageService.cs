using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramInsuranceBot.Application.Interfaces;

namespace TelegramInsuranceBot.Infrastructure.Services
{
    public class DocumentStorageService : IDocumentStorageService
    {
        private readonly TelegramBotClient _botClient;

        public DocumentStorageService(TelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task<string> SavePhotoAsync(string fileId, string type, long userId)
        {
            // Створюємо директорію для збереження файлів користувача
            string baseDir = Path.Combine("Documents", userId.ToString());
            Directory.CreateDirectory(baseDir);

            // Унікальне ім’я файлу
            string fileName = $"{type}_{Guid.NewGuid():N}.jpg";
            string filePath = Path.Combine(baseDir, fileName);

            // Завантажуємо файл з Telegram
            var file = await _botClient.GetFileAsync(fileId);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await _botClient.DownloadFileAsync(file.FilePath, stream);
            }

            return filePath;
        }
    }
}
