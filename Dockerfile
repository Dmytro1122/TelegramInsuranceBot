# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TelegramInsuranceBot.ConsoleApp/*.csproj TelegramInsuranceBot.ConsoleApp/
RUN dotnet restore TelegramInsuranceBot.ConsoleApp/TelegramInsuranceBot.ConsoleApp.csproj

COPY . .
WORKDIR /src/TelegramInsuranceBot.ConsoleApp
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "TelegramInsuranceBot.ConsoleApp.dll"]

