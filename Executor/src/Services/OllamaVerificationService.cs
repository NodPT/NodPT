using BackendExecutor.Config;
using NodPT.Data.DTOs;
using System.Text;
using System.Text.Json;

namespace BackendExecutor.Services;

/// <summary>
/// Service to verify Ollama endpoint connectivity at startup
/// </summary>
public class OllamaVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ExecutorOptions _options;
    private readonly ILogger<OllamaVerificationService> _logger;

    public OllamaVerificationService(
        HttpClient httpClient,
        ExecutorOptions options,
        ILogger<OllamaVerificationService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Verify Ollama endpoint is accessible by sending a test message
    /// </summary>
    /// <returns>True if verification successful, false otherwise</returns>
    public async Task<bool> VerifyConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== Starting Ollama Endpoint Verification ===");
            _logger.LogInformation("Testing endpoint: {Endpoint}", _options.LlmEndpoint);

            // Prepare a simple test request
            var testRequest = new OllamaRequest
            {
                model = "llama3.2:3b", // Use a common small model for testing
                messages = new List<OllamaMessage>
                {
                    new OllamaMessage { role = "user", content = "Hello" }
                },
                options = new OllamaOptions
                {
                    NumPredict = 10, // Limit response tokens for quick test
                    Temperature = 0.7
                }
            };

            var json = JsonSerializer.Serialize(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending test message: 'Hello'");
            
            // Set a reasonable timeout for the test
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await _httpClient.PostAsync(_options.LlmEndpoint, content, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama endpoint returned non-success status: {StatusCode}", response.StatusCode);
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Error response: {ErrorContent}", errorContent);
                return false;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject == null || string.IsNullOrEmpty(responseObject.Content))
            {
                _logger.LogWarning("Ollama endpoint returned empty or invalid response");
                return false;
            }

            _logger.LogInformation("=== Ollama Verification Successful ===");
            _logger.LogInformation("Received response from Ollama: {ResponseLength} characters", responseObject.Content.Length);
            _logger.LogDebug("Response preview: {ResponsePreview}", 
                responseObject.Content.Length > 100 ? responseObject.Content.Substring(0, 100) + "..." : responseObject.Content);
            
            return true;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Ollama endpoint verification timed out after 30 seconds: {Message}", ex.Message);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while verifying Ollama endpoint. Ensure Ollama container is running and accessible.");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error while processing Ollama verification response");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Ollama verification");
            return false;
        }
    }

    /// <summary>
    /// Verify Ollama endpoint with retry logic
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="retryDelaySeconds">Delay between retries in seconds</param>
    /// <returns>True if verification successful, false otherwise</returns>
    public async Task<bool> VerifyConnectionWithRetryAsync(
        int maxRetries = 3,
        int retryDelaySeconds = 5,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            _logger.LogInformation("Ollama verification attempt {Attempt} of {MaxRetries}", attempt, maxRetries);
            
            var success = await VerifyConnectionAsync(cancellationToken);
            
            if (success)
            {
                return true;
            }

            if (attempt < maxRetries)
            {
                _logger.LogWarning("Verification failed. Retrying in {Delay} seconds...", retryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
            }
        }

        _logger.LogError("=== Ollama Verification Failed ===");
        _logger.LogError("Failed to verify Ollama endpoint after {MaxRetries} attempts", maxRetries);
        _logger.LogError("Please ensure:");
        _logger.LogError("  1. Ollama container is running (docker ps | grep ollama)");
        _logger.LogError("  2. Ollama is accessible on the network (curl http://ollama:11434/api/tags)");
        _logger.LogError("  3. Ollama has OLLAMA_HOST=0.0.0.0:11434 environment variable set");
        _logger.LogError("  4. Ollama has OLLAMA_ORIGINS=* environment variable set");
        _logger.LogError("  5. The model '{Model}' is available (ollama list)", "llama3.2:3b");
        
        return false;
    }
}
