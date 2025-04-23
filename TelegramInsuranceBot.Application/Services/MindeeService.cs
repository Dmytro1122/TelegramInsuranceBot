using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TelegramInsuranceBot.Application.Interfaces;
using TelegramInsuranceBot.Domain.Models;

namespace TelegramInsuranceBot.Infrastructure.Services
{
    public class MindeeService : IMindeeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _idCardEndpoint;
        private readonly string _vehicleCardEndpoint;

        public MindeeService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Mindee:ApiKey"];
            _idCardEndpoint = config["Mindee:IdCardEndpoint"];
            _vehicleCardEndpoint = config["Mindee:VehicleCardEndpoint"];
        }

        public async Task<MindeeResult> ProcessIdCardAsync(string filePath)
        {
            return await ProcessDocumentAsync(filePath, _idCardEndpoint, "first_name", "last_name", "id_number");
        }

        public async Task<MindeeResult> ProcessVehicleCardAsync(string filePath)
        {
            return await ProcessDocumentAsync(filePath, _vehicleCardEndpoint, "vehicle_make", "vehicle_model", "vehicle_identification_number");
        }

        private async Task<MindeeResult> ProcessDocumentAsync(string filePath, string endpoint, string key1, string key2, string key3)
        {
            using var form = new MultipartFormDataContent();
            form.Headers.ContentType.MediaType = "multipart/form-data";

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            form.Add(fileContent, "document", Path.GetFileName(filePath));

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = form
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);

            var postResponse = await _httpClient.SendAsync(request);
            if (!postResponse.IsSuccessStatusCode)
                throw new Exception($"Помилка при надсиланні документа: {postResponse.StatusCode}");

            var postJson = await postResponse.Content.ReadAsStringAsync();
            Console.WriteLine("📦 POST-відповідь від Mindee:");
            Console.WriteLine(postJson);

            var postDoc = JsonDocument.Parse(postJson);
            var pollingUrl = postDoc.RootElement
                .GetProperty("job")
                .GetProperty("polling_url")
                .GetString();

            Console.WriteLine("📡 Polling URL: " + pollingUrl);

            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(1500);

                var getRequest = new HttpRequestMessage(HttpMethod.Get, pollingUrl);
                getRequest.Headers.TryAddWithoutValidation("Authorization", $"Token {_apiKey}");

                Console.WriteLine("🛂 GET Headers:");
                foreach (var h in getRequest.Headers)
                {
                    Console.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}");
                }

                var getResponse = await _httpClient.SendAsync(getRequest);

                var getJson = await getResponse.Content.ReadAsStringAsync();
                Console.WriteLine("📦 Polling response from Mindee:");
                Console.WriteLine(getJson);

                var doc = JsonDocument.Parse(getJson);

                if (!doc.RootElement.TryGetProperty("job", out var jobElement) ||
                    !jobElement.TryGetProperty("status", out var statusElement))
                {
                    throw new Exception("❌ Невірна структура JSON: немає поля job.status");
                }

                var status = statusElement.GetString();

                if (status == "done")
                {
                    if (!doc.RootElement.TryGetProperty("document", out var documentElement) ||
                        !documentElement.TryGetProperty("inference", out var inferenceElement) ||
                        !inferenceElement.TryGetProperty("fields", out var fields))
                    {
                        throw new Exception("Неможливо знайти поля в результаті Mindee.");
                    }

                    var result = new MindeeResult();

                    if (fields.TryGetProperty(key1, out var f1))
                        AssignField(result, key1, f1.GetProperty("value").GetString());

                    if (fields.TryGetProperty(key2, out var f2))
                        AssignField(result, key2, f2.GetProperty("value").GetString());

                    if (fields.TryGetProperty(key3, out var f3))
                        AssignField(result, key3, f3.GetProperty("value").GetString());

                    return result;
                }

                if (status == "failed")
                    throw new Exception("Обробка документа не вдалася.");
            }

            throw new Exception("Час очікування обробки документа вичерпано.");
        }




        private void AssignField(MindeeResult result, string fieldName, string? value)
        {
            switch (fieldName)
            {
                case "first_name": result.FirstName = value; break;
                case "last_name": result.LastName = value; break;
                case "id_number": result.IdNumber = value; break;
                case "vehicle_make": result.VehicleMake = value; break;
                case "vehicle_model": result.VehicleModel = value; break;
                case "vehicle_identification_number": result.Vin = value; break;
            }
        }
    }
}
