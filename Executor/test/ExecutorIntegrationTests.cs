using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace BackendExecutor.Tests;

/// <summary>
/// Integration tests for the Executor workflow - simplified Ollama connectivity test
/// </summary>
public class ExecutorIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly IConfiguration _configuration;
    
    public ExecutorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Load test configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }
    
    [Fact]
    public async Task TestOllama()
    {
        _output.WriteLine("=== Ollama Integration Test ===");
        _output.WriteLine("");
        
        // Get configuration
        var ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama:11434";
        var ollamaEndpoint = _configuration["Ollama:GenerateEndpoint"] ?? $"{ollamaBaseUrl}/api/generate";
        var model = _configuration["Ollama:DefaultModel"] ?? "deepseek-r1:1.5b";
        
        _output.WriteLine("=== Configuration ===");
        _output.WriteLine($"Ollama Base URL: {ollamaBaseUrl}");
        _output.WriteLine($"Ollama Endpoint: {ollamaEndpoint}");
        _output.WriteLine($"Model: {model}");
        _output.WriteLine("");
        Console.WriteLine("=== Configuration ===");
        Console.WriteLine($"Ollama Base URL: {ollamaBaseUrl}");
        Console.WriteLine($"Ollama Endpoint: {ollamaEndpoint}");
        Console.WriteLine($"Model: {model}");
        Console.WriteLine("");
        
        // Step 1: Test sending hello message using HttpClient
        _output.WriteLine("=== STEP 1: Send Hello Message using HttpClient ===");
        _output.WriteLine("");
        Console.WriteLine("=== STEP 1: Send Hello Message using HttpClient ===");
        Console.WriteLine("");
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        
        var testRequest = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = "Say hello!" }
            },
            stream = false,
            options = new
            {
                temperature = 0.7,
                num_predict = 50
            }
        };
        
        var jsonContent = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        _output.WriteLine("Request Payload:");
        _output.WriteLine(jsonContent);
        _output.WriteLine("");
        Console.WriteLine("Request Payload:");
        Console.WriteLine(jsonContent);
        Console.WriteLine("");
        
        try
        {
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            _output.WriteLine($"Sending POST request to: {ollamaEndpoint}");
            Console.WriteLine($"Sending POST request to: {ollamaEndpoint}");
            
            using var response = await httpClient.PostAsync(ollamaEndpoint, content);
            
            _output.WriteLine($"Response Status Code: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Response Status Code: {(int)response.StatusCode} {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine("Response Body:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            Console.WriteLine("");
            
            response.EnsureSuccessStatusCode();
            
            // Parse response
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;
            
            string? messageContent = null;
            
            if (root.TryGetProperty("message", out var messageElement))
            {
                if (messageElement.TryGetProperty("content", out var contentElement))
                {
                    messageContent = contentElement.GetString();
                }
            }
            else if (root.TryGetProperty("response", out var responseElement))
            {
                messageContent = responseElement.GetString();
            }
            
            _output.WriteLine("✓ HttpClient test passed");
            _output.WriteLine($"AI Response: {messageContent ?? "N/A"}");
            _output.WriteLine("");
            Console.WriteLine("✓ HttpClient test passed");
            Console.WriteLine($"AI Response: {messageContent ?? "N/A"}");
            Console.WriteLine("");
            
            Assert.NotNull(messageContent);
            Assert.False(string.IsNullOrWhiteSpace(messageContent), "Response should contain content");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ HttpClient test failed: {ex.Message}");
            _output.WriteLine($"Exception Type: {ex.GetType().Name}");
            _output.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine($"✗ HttpClient test failed: {ex.Message}");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
        
        // Step 2: Create curl string and execute it
        _output.WriteLine("=== STEP 2: Generate and Execute Curl Command ===");
        _output.WriteLine("");
        Console.WriteLine("=== STEP 2: Generate and Execute Curl Command ===");
        Console.WriteLine("");
        
        // Create curl command - using single quotes for JSON payload to avoid escaping issues
        var curlCommand = $"curl -X POST {ollamaEndpoint} -H \"Content-Type: application/json\" -d '{jsonContent}'";
        
        _output.WriteLine("Curl Command:");
        _output.WriteLine(curlCommand);
        _output.WriteLine("");
        Console.WriteLine("Curl Command:");
        Console.WriteLine(curlCommand);
        Console.WriteLine("");
        
        // Execute curl command
        _output.WriteLine("Executing curl command...");
        Console.WriteLine("Executing curl command...");
        
        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{curlCommand.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null)
            {
                _output.WriteLine("✗ Failed to start curl process");
                Console.WriteLine("✗ Failed to start curl process");
                return;
            }
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            _output.WriteLine($"Curl Exit Code: {process.ExitCode}");
            Console.WriteLine($"Curl Exit Code: {process.ExitCode}");
            
            if (!string.IsNullOrEmpty(output))
            {
                _output.WriteLine("Curl Output:");
                _output.WriteLine(output);
                _output.WriteLine("");
                Console.WriteLine("Curl Output:");
                Console.WriteLine(output);
                Console.WriteLine("");
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                _output.WriteLine("Curl Error:");
                _output.WriteLine(error);
                _output.WriteLine("");
                Console.WriteLine("Curl Error:");
                Console.WriteLine(error);
                Console.WriteLine("");
            }
            
            if (process.ExitCode == 0)
            {
                _output.WriteLine("✓ Curl command executed successfully");
                Console.WriteLine("✓ Curl command executed successfully");
                
                // Try to parse the curl response
                if (!string.IsNullOrEmpty(output))
                {
                    try
                    {
                        using var curlJsonDoc = JsonDocument.Parse(output);
                        var curlRoot = curlJsonDoc.RootElement;
                        
                        string? curlMessageContent = null;
                        
                        if (curlRoot.TryGetProperty("message", out var curlMessageElement))
                        {
                            if (curlMessageElement.TryGetProperty("content", out var curlContentElement))
                            {
                                curlMessageContent = curlContentElement.GetString();
                            }
                        }
                        else if (curlRoot.TryGetProperty("response", out var curlResponseElement))
                        {
                            curlMessageContent = curlResponseElement.GetString();
                        }
                        
                        if (!string.IsNullOrEmpty(curlMessageContent))
                        {
                            _output.WriteLine($"Curl AI Response: {curlMessageContent}");
                            Console.WriteLine($"Curl AI Response: {curlMessageContent}");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _output.WriteLine($"Could not parse curl response as JSON: {parseEx.Message}");
                        Console.WriteLine($"Could not parse curl response as JSON: {parseEx.Message}");
                    }
                }
            }
            else
            {
                _output.WriteLine($"✗ Curl command failed with exit code {process.ExitCode}");
                Console.WriteLine($"✗ Curl command failed with exit code {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Exception executing curl: {ex.Message}");
            Console.WriteLine($"✗ Exception executing curl: {ex.Message}");
        }
        
        _output.WriteLine("");
        _output.WriteLine("=== Test Completed Successfully ===");
        Console.WriteLine("");
        Console.WriteLine("=== Test Completed Successfully ===");
    }
}
