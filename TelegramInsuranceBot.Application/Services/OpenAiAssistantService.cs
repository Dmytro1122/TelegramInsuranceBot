using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TelegramInsuranceBot.Application.Interfaces;

namespace TelegramInsuranceBot.Infrastructure.Services
{
    public class OpenAiAssistantService : IOpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _assistantId;

        public OpenAiAssistantService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAi:ApiKey"];
            _assistantId = configuration["OpenAi:AssistantId"];
        }

        public async Task<string> AskAsync(string prompt)
        {
            await Task.Delay(2000); // Уникнення 429

            // 1. Створити thread
            var threadResponse = await CreateThreadAsync();
            string threadId = threadResponse.GetProperty("id").GetString();

            // 2. Додати повідомлення до thread
            await PostMessageAsync(threadId, prompt);

            // 3. Запустити run
            var runId = await CreateRunAsync(threadId);

            // 4. Дочекатись завершення run
            await WaitForRunCompletionAsync(threadId, runId);

            // 5. Отримати останнє повідомлення (від GPT)
            var reply = await GetLastMessageAsync(threadId);
            return reply;
        }

        private async Task<JsonElement> CreateThreadAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/threads");
            AddHeaders(request);

            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement;
        }

        private async Task PostMessageAsync(string threadId, string prompt)
        {
            var payload = new
            {
                role = "user",
                content = prompt
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.openai.com/v1/threads/{threadId}/messages"
            );
            AddHeaders(request);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> CreateRunAsync(string threadId)
        {
            var payload = new
            {
                assistant_id = _assistantId
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.openai.com/v1/threads/{threadId}/runs"
            );
            AddHeaders(request);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("id").GetString();
        }

        private async Task WaitForRunCompletionAsync(string threadId, string runId)
        {
            while (true)
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}"
                );
                AddHeaders(request);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var status = JsonDocument.Parse(json).RootElement.GetProperty("status").GetString();

                if (status == "completed")
                    break;

                if (status == "failed" || status == "expired" || status == "cancelled")
                    throw new Exception($"Run failed with status: {status}");

                await Task.Delay(1000); // Чекаємо
            }
        }

        private async Task<string> GetLastMessageAsync(string threadId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.openai.com/v1/threads/{threadId}/messages"
            );
            AddHeaders(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var messages = doc.RootElement.GetProperty("data");

            foreach (var message in messages.EnumerateArray())
            {
                if (message.GetProperty("role").GetString() == "assistant")
                {
                    return message.GetProperty("content")[0].GetProperty("text").GetProperty("value").GetString();
                }
            }

            return "Помилка: GPT не відповів.";
        }

        private void AddHeaders(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("OpenAI-Beta", "assistants=v2");
        }
    }
}

