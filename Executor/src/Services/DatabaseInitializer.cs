using Microsoft.Extensions.Hosting;

namespace BackendExecutor.Services;

public static class DatabaseInitializer
{
    public static void Initialize(HostApplicationBuilder builder)
    {
        // Parameter kept for API consistency with WebAPI's DatabaseInitializer, which uses the builder for DI.
        _ = builder;
        // Do NOT use defaults. Require all parts to be provided via env vars or configuration.
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var db = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        
        // Validate required database environment variables
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || 
            string.IsNullOrEmpty(db) || string.IsNullOrEmpty(user) || 
            string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database configuration is incomplete. Required environment variables: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD");
        }
        
        var connectionString = $"XpoProvider=MySql;server={host};port={port};user={user};password={password};database={db};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
        DatabaseHelper.SetConnectionString(connectionString);
    }
}
