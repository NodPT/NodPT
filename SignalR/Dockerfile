# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file from the new structure and restore dependencies
COPY src/NodPT.SignalR.csproj ./
RUN dotnet restore NodPT.SignalR.csproj

# Copy all source files
COPY src/. ./

# Publish the SignalR application
RUN dotnet publish NodPT.SignalR.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5288
EXPOSE 5288

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "NodPT.SignalR.dll"]
