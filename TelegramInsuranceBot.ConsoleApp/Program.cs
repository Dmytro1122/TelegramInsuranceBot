using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramInsuranceBot.Application.Interfaces;
using TelegramInsuranceBot.Application.Services;
using TelegramInsuranceBot.Infrastructure.Services;

namespace TelegramInsuranceBot.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration;
                    string botToken = config["TelegramBot__Token"];

                    services.AddSingleton<TelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddSingleton<IBotConversationHandler, BotConversationHandler>();
                    services.AddHttpClient<IOpenAiService, OpenAiAssistantService>();
                    services.AddScoped<IDocumentStorageService, DocumentStorageService>();
                    services.AddHttpClient<IMindeeService, MindeeService>();
                    services.AddSingleton<IInsurancePolicyGenerator, InsurancePolicyGenerator>();
                    services.AddSingleton<IPolicyGeneratorService, PolicyGeneratorService>();
                })
                .Build();

            var botHandler = host.Services.GetRequiredService<IBotConversationHandler>();
            await botHandler.StartAsync();

            Console.WriteLine("BOT STARTED OK");

            Console.ReadLine();
        }
    }
}

