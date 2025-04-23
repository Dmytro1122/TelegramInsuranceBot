namespace TelegramInsuranceBot.Domain.Models
{
    public class UserSession
    {
        public string? PassportPath { get; set; }
        public string? VehiclePath { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }

        public bool IsAwaitingConfirmation { get; set; }
        public bool IsAwaitingPaymentConfirmation { get; set; }

        public MindeeResult? IdCardData { get; set; }
        public MindeeResult? VehicleCardData { get; set; }
    }
}
