# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy API and Data project files from the repository root layout
COPY WebAPI/src/NodPT.API.csproj WebAPI/src/
COPY Data/src/NodPT.Data.csproj Data/src/

# Restore dependencies for the API (will also restore the Data project reference)
RUN dotnet restore WebAPI/src/NodPT.API.csproj

# Copy the remaining source code for both projects
COPY WebAPI/src/ WebAPI/src/
COPY Data/src/ Data/src/

# Build the API project
RUN dotnet build WebAPI/src/NodPT.API.csproj -c Release -o /app/build

# ===== Publish stage =====
FROM build AS publish
RUN dotnet publish WebAPI/src/NodPT.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8846

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "NodPT.API.dll"]
