# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy Executor and Data project files from the repository root layout
COPY Executor/src/Executor.csproj Executor/src/
COPY Data/src/NodPT.Data.csproj Data/src/
COPY Utils/src/NodPT.Utils.csproj Utils/src/

# Restore dependencies for the Executor (will also restore the Data project reference)
RUN dotnet restore Executor/src/Executor.csproj

# Copy the remaining source code for both projects
COPY Executor/src/ Executor/src/
COPY Data/src/ Data/src/
COPY Utils/src/ Utils/src/

# Build the Executor project
RUN dotnet build Executor/src/Executor.csproj -c Release -o /app/build

# ===== Publish stage =====
FROM build AS publish
RUN dotnet publish Executor/src/Executor.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for connectivity testing
RUN apt-get update && apt-get install -y curl jq && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish ./

# # Copy the Ollama connectivity test script
# COPY Executor/test-ollama-connectivity.sh /app/test-ollama-connectivity.sh
# RUN chmod +x /app/test-ollama-connectivity.sh

ENTRYPOINT ["dotnet", "Executor.dll"]
