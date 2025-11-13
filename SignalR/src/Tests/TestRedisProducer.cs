using StackExchange.Redis;
using System.Text.Json;

namespace NodPT.SignalR.Tests;

/// <summary>
/// Test utility to simulate the Executor writing messages to Redis stream.
/// This is for testing purposes only and demonstrates how the Executor should write to Redis.
/// </summary>
public class TestRedisProducer
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Redis Test Producer - Simulating Executor");
        Console.WriteLine("==========================================");
        
        var connectionString = args.Length > 0 ? args[0] : "localhost:6379";
        Console.WriteLine($"Connecting to Redis at: {connectionString}");
        
        var redis = ConnectionMultiplexer.Connect(connectionString);
        var db = redis.GetDatabase();
        
        Console.WriteLine("Connected to Redis successfully!");
        Console.WriteLine("\nWriting test messages to 'signalr:updates' stream...\n");
        
        // Test message 1: Route by UserId
        var messageId1 = await db.StreamAddAsync("signalr:updates", new[]
        {
            new NameValueEntry("MessageId", Guid.NewGuid().ToString()),
            new NameValueEntry("NodeId", "node-001"),
            new NameValueEntry("ProjectId", "project-123"),
            new NameValueEntry("UserId", "test-user-1"),
            new NameValueEntry("ClientConnectionId", ""),
            new NameValueEntry("WorkflowGroup", ""),
            new NameValueEntry("Type", "result"),
            new NameValueEntry("Payload", JsonSerializer.Serialize(new { result = "success", data = "Test data for user routing" })),
            new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("o"))
        });
        Console.WriteLine($"✓ Message 1 written (UserId routing): {messageId1}");
        
        // Test message 2: Route by WorkflowGroup
        var messageId2 = await db.StreamAddAsync("signalr:updates", new[]
        {
            new NameValueEntry("MessageId", Guid.NewGuid().ToString()),
            new NameValueEntry("NodeId", "node-002"),
            new NameValueEntry("ProjectId", "project-456"),
            new NameValueEntry("UserId", "test-user-2"),
            new NameValueEntry("ClientConnectionId", ""),
            new NameValueEntry("WorkflowGroup", "workflow-alpha"),
            new NameValueEntry("Type", "result"),
            new NameValueEntry("Payload", JsonSerializer.Serialize(new { result = "processing", progress = 50 })),
            new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("o"))
        });
        Console.WriteLine($"✓ Message 2 written (WorkflowGroup routing): {messageId2}");
        
        // Test message 3: Route by specific ClientConnectionId (if you know one)
        var messageId3 = await db.StreamAddAsync("signalr:updates", new[]
        {
            new NameValueEntry("MessageId", Guid.NewGuid().ToString()),
            new NameValueEntry("NodeId", "node-003"),
            new NameValueEntry("ProjectId", "project-789"),
            new NameValueEntry("UserId", "test-user-3"),
            new NameValueEntry("ClientConnectionId", ""),  // Empty - would route to UserId
            new NameValueEntry("WorkflowGroup", ""),
            new NameValueEntry("Type", "result"),
            new NameValueEntry("Payload", JsonSerializer.Serialize(new { result = "completed", output = "Final result" })),
            new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("o"))
        });
        Console.WriteLine($"✓ Message 3 written (UserId routing): {messageId3}");
        
        Console.WriteLine("\n✅ All test messages written successfully!");
        Console.WriteLine("Check your SignalR clients to see if they received the messages.");
        
        redis.Dispose();
    }
}
