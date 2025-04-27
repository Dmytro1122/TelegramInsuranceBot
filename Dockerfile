# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY TelegramInsuranceBot.ConsoleApp/*.csproj TelegramInsuranceBot.ConsoleApp/
RUN dotnet restore TelegramInsuranceBot.ConsoleApp/TelegramInsuranceBot.ConsoleApp.csproj

# Copy everything else
COPY . .
WORKDIR /src/TelegramInsuranceBot.ConsoleApp

# Publish app
RUN dotnet publish -c Release -o /app

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Launch
ENTRYPOINT ["dotnet", "TelegramInsuranceBot.ConsoleApp.dll"]
