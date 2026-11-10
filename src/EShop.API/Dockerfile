# EShop DDD Demo API - Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/EShop.API/EShop.API.csproj", "src/EShop.API/"]
COPY ["src/EShop.Application/EShop.Application.csproj", "src/EShop.Application/"]
COPY ["src/EShop.Domain/EShop.Domain.csproj", "src/EShop.Domain/"]
COPY ["src/EShop.Infrastructure/EShop.Infrastructure.csproj", "src/EShop.Infrastructure/"]
RUN dotnet restore "src/EShop.API/EShop.API.csproj"
COPY . .
WORKDIR "/src/src/EShop.API"
RUN dotnet build "EShop.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EShop.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShop.API.dll"]
