# PRODUCTION DOCKERFILE
# Multi-stage build for minimal image size

# ================================
# Build Stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (for layer caching)
COPY ["src/EShop.Domain/EShop.Domain.csproj", "EShop.Domain/"]
COPY ["src/EShop.Application/EShop.Application.csproj", "EShop.Application/"]
COPY ["src/EShop.Infrastructure/EShop.Infrastructure.csproj", "EShop.Infrastructure/"]
COPY ["src/EShop.API/EShop.API.csproj", "EShop.API/"]

# Restore dependencies
RUN dotnet restore "EShop.API/EShop.API.csproj"

# Copy source code
COPY src/ .

# Build
RUN dotnet build "EShop.API/EShop.API.csproj" -c Release -o /app/build

# ================================
# Publish Stage
# ================================
FROM build AS publish
RUN dotnet publish "EShop.API/EShop.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ================================
# Runtime Stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: Run as non-root user
RUN adduser --disabled-password --gecos "" appuser
USER appuser

# Copy published files
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "EShop.API.dll"]
