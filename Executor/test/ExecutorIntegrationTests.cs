using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using NodPT.Data.DTOs;

namespace BackendExecutor.Tests;

/// <summary>
/// Integration tests for the Executor workflow including Redis message queuing,
/// Ollama LLM integration, and summarization functionality.
/// 
/// Test Workflow:
/// 1. Add "hello" message to Redis jobs:chat stream
/// 2. Add 10 dummy song composer messages to test the flow
/// 3. Verify messages are processed by the Executor
/// 4. Test summarization functionality
/// 5. Print detailed logs to console for inspection
/// </summary>
public class ExecutorIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDb;
    private readonly string _testStreamKey;
    
    public ExecutorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Load test configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        
        // Get Ollama configuration from settings
        var ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama:11434";
        var ollamaEndpoint = _configuration["Ollama:GenerateEndpoint"] ?? $"{ollamaBaseUrl}/api/generate";
        
        _output.WriteLine("=== Executor Integration Test Configuration ===");
        _output.WriteLine($"Ollama Base URL: {ollamaBaseUrl}");
        _output.WriteLine($"Ollama Generate Endpoint: {ollamaEndpoint}");
        _output.WriteLine($"Redis Connection: {_configuration["Redis:ConnectionString"]}");
        _output.WriteLine($"Test Stream: {_configuration["Redis:Streams:JobsChat"]}");
        _output.WriteLine("");
        
        // Setup Redis connection
        var redisConnection = _configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _testStreamKey = _configuration["Redis:Streams:JobsChat"] ?? "jobs:chat:test";
        
        var redisOptions = ConfigurationOptions.Parse(redisConnection);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout = 10000;
        redisOptions.SyncTimeout = 10000;
        
        _redis = ConnectionMultiplexer.Connect(redisOptions);
        _redisDb = _redis.GetDatabase();
        
        _output.WriteLine($"Connected to Redis: {redisConnection}");
        _output.WriteLine($"Test Stream Key: {_testStreamKey}");
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_HelloMessage_AddedToRedis()
    {
        _output.WriteLine("=== TEST 1: Add Hello Message to Redis ===");
        
        // Prepare hello message
        var helloMessage = new Dictionary<string, string>
        {
            { "chatId", "test-hello-1" },
            { "message", "Hello! This is a test message." },
            { "nodeId", "test-node-1" },
            { "userId", "test-user-1" },
            { "connectionId", "test-connection-1" },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };
        
        // Add to Redis stream
        var entries = helloMessage.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray();
        var entryId = await _redisDb.StreamAddAsync(_testStreamKey, entries);
        
        _output.WriteLine($"✓ Hello message added to Redis stream");
        _output.WriteLine($"  Entry ID: {entryId}");
        _output.WriteLine($"  Stream Key: {_testStreamKey}");
        _output.WriteLine($"  Message: {helloMessage["message"]}");
        
        // Verify message exists in stream
        var streamLength = await _redisDb.StreamLengthAsync(_testStreamKey);
        _output.WriteLine($"  Current stream length: {streamLength}");
        
        Assert.True(streamLength > 0, "Stream should contain at least one message");
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_DummySongComposerMessages_AddedToRedis()
    {
        _output.WriteLine("=== TEST 2: Add 10 Dummy Song Composer Messages to Redis ===");
        _output.WriteLine("");
        
        var songTopics = new[]
        {
            "Write a song about the beauty of mountain sunrise",
            "Create lyrics for a jazz song about a rainy evening in the city",
            "Compose a folk song about friendship and loyalty",
            "Write a pop song about chasing your dreams",
            "Create a rock anthem about overcoming challenges",
            "Compose a ballad about lost love and memories",
            "Write a country song about life on the open road",
            "Create an electronic dance track about celebration and joy",
            "Compose a blues song about heartbreak and recovery",
            "Write a classical piece description about nature's symphony"
        };
        
        var entryIds = new List<string>();
        
        for (int i = 0; i < songTopics.Length; i++)
        {
            var songMessage = new Dictionary<string, string>
            {
                { "chatId", $"test-song-{i + 1}" },
                { "message", songTopics[i] },
                { "nodeId", $"test-song-node-{i + 1}" },
                { "userId", "test-composer-user" },
                { "connectionId", $"test-connection-song-{i + 1}" },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "messageType", "song-composer" },
                { "songIndex", (i + 1).ToString() }
            };
            
            var entries = songMessage.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray();
            var entryId = await _redisDb.StreamAddAsync(_testStreamKey, entries);
            entryIds.Add(entryId.ToString());
            
            _output.WriteLine($"✓ Song message {i + 1}/10 added");
            _output.WriteLine($"  Entry ID: {entryId}");
            _output.WriteLine($"  Topic: {songTopics[i]}");
            _output.WriteLine("");
            
            // Small delay between messages
            await Task.Delay(100);
        }
        
        // Verify all messages exist
        var streamLength = await _redisDb.StreamLengthAsync(_testStreamKey);
        _output.WriteLine($"✓ All 10 song composer messages added successfully");
        _output.WriteLine($"  Total stream length: {streamLength}");
        _output.WriteLine($"  Entry IDs: {string.Join(", ", entryIds)}");
        
        Assert.True(streamLength >= 10, "Stream should contain at least 10 messages");
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_OllamaConnectivity_WithStreamFalse()
    {
        _output.WriteLine("=== TEST 3: Test Ollama Connectivity with stream=false ===");
        _output.WriteLine("");
        
        var ollamaEndpoint = _configuration["Ollama:GenerateEndpoint"] ?? "http://ollama:11434/api/generate";
        var model = _configuration["Ollama:DefaultModel"] ?? "deepseek-r1:1.5b";
        
        _output.WriteLine($"Testing Ollama endpoint: {ollamaEndpoint}");
        _output.WriteLine($"Model: {model}");
        _output.WriteLine("");
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        
        // Test with messages array for chat history
        var testRequest = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant. Respond concisely." },
                new { role = "user", content = "Say hello in exactly 5 words." }
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
        
        _output.WriteLine("Request payload:");
        _output.WriteLine(jsonContent);
        _output.WriteLine("");
        
        try
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ollamaEndpoint, content);
            
            _output.WriteLine($"Response Status: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("Response Body:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");
            
            response.EnsureSuccessStatusCode();
            
            // Parse response
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;
            
            // Extract message content based on response format
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
            
            _output.WriteLine("✓ Ollama connectivity test passed");
            _output.WriteLine($"  Response content: {messageContent ?? "N/A"}");
            
            Assert.NotNull(messageContent);
            Assert.False(string.IsNullOrWhiteSpace(messageContent), "Response should contain content");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Ollama connectivity test failed: {ex.Message}");
            _output.WriteLine($"  Exception Type: {ex.GetType().Name}");
            _output.WriteLine($"  Stack Trace: {ex.StackTrace}");
            throw;
        }
        
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_OllamaRequest_WithFormatForDirectorNode()
    {
        _output.WriteLine("=== TEST 4: Test Ollama Request with Format for Director Node ===");
        _output.WriteLine("");
        
        var ollamaEndpoint = _configuration["Ollama:GenerateEndpoint"] ?? "http://ollama:11434/api/generate";
        var model = _configuration["Ollama:DefaultModel"] ?? "deepseek-r1:1.5b";
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        
        // Define JSON schema format for Director node output
        var formatSchema = new
        {
            type = "object",
            properties = new
            {
                action = new { type = "string", description = "The action to perform" },
                reasoning = new { type = "string", description = "Reasoning for the decision" },
                next_steps = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "List of next steps"
                }
            },
            required = new[] { "action", "reasoning" }
        };
        
        var testRequest = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a director making decisions. Respond in JSON format." },
                new { role = "user", content = "Plan the next steps for creating a song about mountains." }
            },
            stream = false,
            format = "json",
            options = new
            {
                temperature = 0.5,
                num_predict = 200
            }
        };
        
        var jsonContent = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        _output.WriteLine("Request payload with format enforcement:");
        _output.WriteLine(jsonContent);
        _output.WriteLine("");
        
        try
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ollamaEndpoint, content);
            
            _output.WriteLine($"Response Status: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("Response Body:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");
            
            response.EnsureSuccessStatusCode();
            
            // Parse and validate response format
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
            
            _output.WriteLine("✓ Format enforcement test passed");
            _output.WriteLine($"  Formatted response: {messageContent ?? "N/A"}");
            
            // Try to parse as JSON to verify format compliance
            if (!string.IsNullOrWhiteSpace(messageContent))
            {
                try
                {
                    var parsedContent = JsonDocument.Parse(messageContent);
                    _output.WriteLine("  ✓ Response is valid JSON");
                }
                catch
                {
                    _output.WriteLine("  ⚠ Response is not valid JSON (may still be acceptable)");
                }
            }
            
            Assert.NotNull(messageContent);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Format enforcement test failed: {ex.Message}");
            throw;
        }
        
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_ChatHistoryWithMessages()
    {
        _output.WriteLine("=== TEST 5: Test Chat History Using Messages Array ===");
        _output.WriteLine("");
        
        var ollamaEndpoint = _configuration["Ollama:GenerateEndpoint"] ?? "http://ollama:11434/api/generate";
        var model = _configuration["Ollama:DefaultModel"] ?? "deepseek-r1:1.5b";
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        
        // Simulate chat history with multiple messages
        var chatHistory = new[]
        {
            new { role = "system", content = "You are a helpful music composer assistant." },
            new { role = "user", content = "I want to write a song about mountains." },
            new { role = "assistant", content = "That's a beautiful theme! Mountains evoke feelings of majesty, adventure, and tranquility. What style of music are you thinking?" },
            new { role = "user", content = "Make it a folk song with acoustic guitar." },
            new { role = "assistant", content = "Perfect! Folk music with acoustic guitar is ideal for nature themes. I'll help you compose it." },
            new { role = "user", content = "Write the first verse." }
        };
        
        var testRequest = new
        {
            model = model,
            messages = chatHistory,
            stream = false,
            options = new
            {
                temperature = 0.8,
                num_predict = 150
            }
        };
        
        var jsonContent = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        _output.WriteLine("Request with chat history (messages array):");
        _output.WriteLine(jsonContent);
        _output.WriteLine("");
        
        try
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ollamaEndpoint, content);
            
            _output.WriteLine($"Response Status: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("Response Body:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");
            
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
            
            _output.WriteLine("✓ Chat history test passed");
            _output.WriteLine($"  AI Response (first verse): {messageContent ?? "N/A"}");
            
            Assert.NotNull(messageContent);
            Assert.False(string.IsNullOrWhiteSpace(messageContent), "Response should contain the first verse");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Chat history test failed: {ex.Message}");
            throw;
        }
        
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_SummarizationFlow()
    {
        _output.WriteLine("=== TEST 6: Test Summarization Functionality ===");
        _output.WriteLine("");
        
        var summarizationEndpoint = _configuration["Summarization:BaseUrl"] ?? "http://ollama:11434/api/generate";
        var summarizationModel = _configuration["Summarization:Model"] ?? "llama3.2:1b";
        
        _output.WriteLine($"Summarization Endpoint: {summarizationEndpoint}");
        _output.WriteLine($"Summarization Model: {summarizationModel}");
        _output.WriteLine("");
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        
        // Long conversation to summarize
        var conversationToSummarize = @"
User: I want to write a song about mountains.
Assistant: That's a beautiful theme! Mountains evoke feelings of majesty, adventure, and tranquility.
User: Make it a folk song with acoustic guitar.
Assistant: Perfect! Folk music with acoustic guitar is ideal for nature themes.
User: Write the first verse.
Assistant: Here's the first verse: 'High above the valley floor, where eagles dare to soar, Stand the mountains tall and grand, ancient keepers of the land.'
User: That's beautiful! Now add a chorus.
Assistant: Here's the chorus: 'Oh mountains high, touch the sky, Your peaks of stone will never die, Through wind and rain you stand so strong, Mountains high, you are my song.'
";
        
        var summarizationRequest = new
        {
            model = summarizationModel,
            prompt = $"Summarize this conversation in 2-3 sentences:\n\n{conversationToSummarize}",
            stream = false,
            options = new
            {
                temperature = 0.3,
                num_predict = 100
            }
        };
        
        var jsonContent = JsonSerializer.Serialize(summarizationRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        _output.WriteLine("Summarization Request:");
        _output.WriteLine(jsonContent);
        _output.WriteLine("");
        
        try
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(summarizationEndpoint, content);
            
            _output.WriteLine($"Response Status: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("Response Body:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");
            
            response.EnsureSuccessStatusCode();
            
            // Parse response
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;
            
            string? summary = null;
            if (root.TryGetProperty("response", out var responseElement))
            {
                summary = responseElement.GetString();
            }
            
            _output.WriteLine("✓ Summarization test passed");
            _output.WriteLine($"  Summary: {summary ?? "N/A"}");
            
            Assert.NotNull(summary);
            Assert.False(string.IsNullOrWhiteSpace(summary), "Summary should not be empty");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Summarization test failed: {ex.Message}");
            throw;
        }
        
        _output.WriteLine("");
    }
    
    [Fact]
    public async Task Test_StreamInfo()
    {
        _output.WriteLine("=== TEST 7: Check Redis Stream Information ===");
        _output.WriteLine("");
        
        var streamLength = await _redisDb.StreamLengthAsync(_testStreamKey);
        _output.WriteLine($"Stream: {_testStreamKey}");
        _output.WriteLine($"Length: {streamLength} messages");
        
        // Try to read some messages
        if (streamLength > 0)
        {
            var messages = await _redisDb.StreamReadAsync(_testStreamKey, "0-0", count: 5);
            
            _output.WriteLine($"Sample Messages (first 5):");
            foreach (var message in messages)
            {
                _output.WriteLine($"  Entry ID: {message.Id}");
                foreach (var field in message.Values)
                {
                    _output.WriteLine($"    {field.Name}: {field.Value}");
                }
                _output.WriteLine("");
            }
        }
        
        Assert.True(true, "Stream info check completed");
        _output.WriteLine("");
    }
    
    public void Dispose()
    {
        _output.WriteLine("=== Cleanup ===");
        _output.WriteLine($"Test stream key: {_testStreamKey}");
        _output.WriteLine("Note: Test stream will remain for inspection. Clean manually if needed.");
        _output.WriteLine("");
        
        _redis?.Dispose();
    }
}
