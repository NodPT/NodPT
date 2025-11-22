using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodPT.Data;
using NodPT.Data.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;


public static class DatabaseInitializer
{
    static string connectionString = string.Empty;
    public static void Initialize(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
    {
        // Do NOT use defaults. Require all parts to be provided via env vars or configuration.
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var db = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        connectionString = $"Server={host};Port={port};Database={db};User={user};Password={password};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
        DatabaseHelper.SetConnectionString(connectionString);

        // Add Entity Framework Core with MySQL
        builder.Services.AddDbContext<NodPTDbContext>(options =>
        {
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
        });

        // Create sample data
        //CreateSampleData();
    }


}