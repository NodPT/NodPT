using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using RedisService.Cache;
using RedisService.Queue;
using System;

namespace NodPT.Utils
{
    public class Common
    {
        /// <summary>
        /// Setup Redis connection and register related services
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        public static void SetupRedis<T>(IHostApplicationBuilder builder)
        {
            // Register Redis
            var redisConnection = builder.Configuration["Redis:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION")
                ?? "nodpt-redis:6379";

            // Add abortConnect=false to allow retry behavior when Redis is unavailable
            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectTimeout = 5000;
            redisOptions.SyncTimeout = 5000;

            builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var logger = provider.GetService<ILogger<T>>();

                try
                {
                    logger?.LogInformation("Connecting to Redis at {RedisConnection}...", redisConnection);
                    var connection = ConnectionMultiplexer.Connect(redisOptions);
                    // ConnectionMultiplexer will handle reconnects in the background (AbortOnConnectFail=false)
                    // No need to force a ping here; rely on built-in retry behavior.
                    return connection;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to connect to Redis at {RedisConnection}. Redis features will be unavailable. Ensure Redis is running and accessible.", redisConnection);
                    // Return connection anyway - it will retry in background with AbortOnConnectFail=false
                    return ConnectionMultiplexer.Connect(redisOptions);
                }
            });

            // Register Redis Cache and Queue Services
            builder.Services.AddSingleton<RedisCacheService>(provider =>
            {
                var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
                var logger = provider.GetRequiredService<ILogger<RedisCacheService>>();
                return new RedisCacheService(multiplexer, logger);
            });

            builder.Services.AddSingleton<RedisQueueService>(provider =>
            {
                var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
                var logger = provider.GetRequiredService<ILogger<RedisQueueService>>();
                return new RedisQueueService(multiplexer, logger);
            });


        }

        /// <summary>
        /// Load environment variables from .env file in Development, or from system environment in Production
        /// </summary>
        /// <param name="builder"></param>
        public static void LoadEnvVariables(IHostApplicationBuilder builder)
        {
            // if the environment is Development, load .env file
            if (builder.Environment.IsDevelopment())
            {
                var path = Path.Combine(AppContext.BaseDirectory, ".env");
                if (File.Exists(path))
                {
                    Console.WriteLine($"Loading .env from {path}");
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim().Trim('"');
                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }
            }
            else
            {
                // 🔹 Load environment variables
                // In production, load environment variables from system environment
                builder.Configuration.AddEnvironmentVariables();
            }
        }
    }
}