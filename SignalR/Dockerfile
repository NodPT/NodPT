# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file from the new structure and restore dependencies
COPY SignalR/src/NodPT.SignalR.csproj SignalR/src/

# Restore dependencies for the API (will also restore the Data project reference)
RUN dotnet restore SignalR/src/NodPT.SignalR.csproj

# Copy all source files
COPY  SignalR/src/ SignalR/src/

# Build the SignalR project
RUN dotnet build SignalR/src/NodPT.SignalR.csproj -c Release -o /app/build

# Publish the SignalR application
RUN dotnet publish SignalR/src/NodPT.SignalR.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8847

# Install curl
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "NodPT.SignalR.dll"]
