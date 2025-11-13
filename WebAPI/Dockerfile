# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy API and Data project files
COPY NodPT.API.csproj .
COPY NodPT.Data/NodPT.Data.csproj NodPT.Data/

# Restore dependencies
RUN dotnet restore NodPT.API.csproj

# Copy all sources (API + Data)
COPY . .

# Build
RUN dotnet build NodPT.API.csproj -c Release -o /app/build

# ===== Publish stage =====
FROM build AS publish
RUN dotnet publish NodPT.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8846

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NodPT.API.dll"]
