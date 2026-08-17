# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["backend/FinanceApp.sln", "."]
COPY ["backend/FinanceApp.Api/FinanceApp.Api.csproj", "FinanceApp.Api/"]
COPY ["backend/FinanceApp.Application/FinanceApp.Application.csproj", "FinanceApp.Application/"]
COPY ["backend/FinanceApp.Domain/FinanceApp.Domain.csproj", "FinanceApp.Domain/"]
COPY ["backend/FinanceApp.Infrastructure/FinanceApp.Infrastructure.csproj", "FinanceApp.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "FinanceApp.Api/FinanceApp.Api.csproj"

# Copy all source code
COPY backend/ .

# Build
WORKDIR "/src/FinanceApp.Api"
RUN dotnet build "FinanceApp.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "FinanceApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FinanceApp.Api.dll"]
