using BackendExecutor.Config;
using BackendExecutor.Services;
using Microsoft.Extensions.Logging;

namespace BackendExecutor.Tests;

/// <summary>
/// Test utility to verify LlmChatService works correctly with an LLM endpoint.
/// This is for testing purposes only and demonstrates how to use LlmChatService.
/// </summary>
public class TestLlmClient
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("LLM Chat Service Test Client");
        Console.WriteLine("============================");
        
        var endpoint = args.Length > 0 ? args[0] : "http://localhost:8355/v1/chat/completions";
        Console.WriteLine($"LLM Endpoint: {endpoint}");
        Console.WriteLine();
        
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        var logger = loggerFactory.CreateLogger<LlmChatService>();
        
        // Create service
        var options = new ExecutorOptions { LlmEndpoint = endpoint };
        var httpClient = new HttpClient();
        var llmService = new LlmChatService(httpClient, options, logger);
        
        try
        {
            // Test 1: Send a simple string message
            Console.WriteLine("Test 1: Sending simple string message...");
            Console.WriteLine("-----------------------------------------");
            var stringMessage = "What is the capital of France?";
            Console.WriteLine($"Message: {stringMessage}");
            
            var response1 = await llmService.SendChatMessageAsync(
                message: stringMessage,
                model: "trt-llm-manager",
                maxTokens: 64
            );
            
            Console.WriteLine($"Response: {response1}");
            Console.WriteLine("✓ Test 1 passed\n");
            
            // Test 2: Send an object message
            Console.WriteLine("Test 2: Sending object message...");
            Console.WriteLine("---------------------------------");
            var objectMessage = new
            {
                task = "code_review",
                language = "csharp",
                code = "public class Example { }"
            };
            Console.WriteLine($"Message Object: {System.Text.Json.JsonSerializer.Serialize(objectMessage)}");
            
            var response2 = await llmService.SendChatMessageAsync(
                messageObject: objectMessage,
                model: "trt-llm-inspector",
                maxTokens: 128
            );
            
            Console.WriteLine($"Response: {response2}");
            Console.WriteLine("✓ Test 2 passed\n");
            
            // Test 3: Test with different model
            Console.WriteLine("Test 3: Testing with agent model...");
            Console.WriteLine("-----------------------------------");
            var agentMessage = "Explain async/await in C#";
            Console.WriteLine($"Message: {agentMessage}");
            
            var response3 = await llmService.SendChatMessageAsync(
                message: agentMessage,
                model: "trt-llm-agent",
                maxTokens: 256
            );
            
            Console.WriteLine($"Response: {response3}");
            Console.WriteLine("✓ Test 3 passed\n");
            
            Console.WriteLine("============================");
            Console.WriteLine("✅ All tests completed successfully!");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n❌ HTTP Error: {ex.Message}");
            Console.WriteLine("\nPossible causes:");
            Console.WriteLine("- LLM endpoint is not running");
            Console.WriteLine("- Incorrect endpoint URL");
            Console.WriteLine("- Network connectivity issues");
            Console.WriteLine($"\nEndpoint: {endpoint}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
        }
        finally
        {
            httpClient.Dispose();
        }
    }
}
