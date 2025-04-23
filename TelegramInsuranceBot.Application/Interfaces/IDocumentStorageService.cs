using System.Threading.Tasks;

namespace TelegramInsuranceBot.Application.Interfaces
{
    public interface IDocumentStorageService
    {
        Task<string> SavePhotoAsync(string fileId, string type, long userId);
    }
}


