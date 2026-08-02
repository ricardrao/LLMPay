# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["LLMPay.Api/LLMPay.Api.csproj", "LLMPay.Api/"]
COPY ["LLMPay.Domain/LLMPay.Domain.csproj", "LLMPay.Domain/"]
COPY ["LLMPay.Core/LLMPay.Core.csproj", "LLMPay.Core/"]
COPY ["LLMPay.Infrastructure/LLMPay.Infrastructure.csproj", "LLMPay.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "LLMPay.Api/LLMPay.Api.csproj"

# Copy all source code
COPY . .

# Build application
RUN dotnet build "LLMPay.Api/LLMPay.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LLMPay.Api/LLMPay.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 80
EXPOSE 443

# Set environment
ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD dotnet /app/LLMPay.Api.dll

# Run application
ENTRYPOINT ["dotnet", "LLMPay.Api.dll"]
