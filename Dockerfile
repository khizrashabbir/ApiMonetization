# Multi-stage Dockerfile for API Monetization Gateway

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["ApiMonetizationGateway.API/ApiMonetizationGateway.API.csproj", "ApiMonetizationGateway.API/"]
COPY ["ApiMonetizationGateway.Application/ApiMonetizationGateway.Application.csproj", "ApiMonetizationGateway.Application/"]
COPY ["ApiMonetizationGateway.Infrastructure/ApiMonetizationGateway.Infrastructure.csproj", "ApiMonetizationGateway.Infrastructure/"]
COPY ["src/ApiMonetizationGateway.Domain/ApiMonetizationGateway.Domain.csproj", "src/ApiMonetizationGateway.Domain/"]

# Restore dependencies
RUN dotnet restore "ApiMonetizationGateway.API/ApiMonetizationGateway.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/ApiMonetizationGateway.API"
RUN dotnet publish "ApiMonetizationGateway.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ApiMonetizationGateway.API.dll"]