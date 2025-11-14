# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Use the .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies from the src folder
COPY src/BackendExecutor.csproj ./
RUN dotnet restore "BackendExecutor.csproj"

# Copy source code and build
COPY src/. ./
RUN dotnet build "BackendExecutor.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "BackendExecutor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

# Set environment variables with defaults
ENV REDIS_CONNECTION=localhost:6379
ENV MAX_MANAGER=0
ENV MAX_INSPECTOR=0
ENV MAX_AGENT=0
ENV MAX_TOTAL=0

ENTRYPOINT ["dotnet", "BackendExecutor.dll"]
