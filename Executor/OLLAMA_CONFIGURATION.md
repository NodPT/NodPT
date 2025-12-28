# Ollama Configuration Guide

## Overview

The Executor application has full support for configurable Ollama endpoints across all configuration files and environment variables.

## Configuration Locations

### 1. Main Application (src/appsettings.json)

The Ollama endpoint is configured in multiple sections:

```json
{
  "Executor": {
    "LlmEndpoint": "http://ollama:11434/api/generate",
    "DefaultModel": "deepseek-r1:1.5b"
  },
  "Summarization": {
    "BaseUrl": "http://ollama:11434/api/generate",
    "Model": "llama3.2:1b",
    "TimeoutSeconds": 60,
    "MaxSummaryLength": 2000
  }
}
```

### 2. Environment Variables

All configuration values can be overridden via environment variables:

```bash
# LLM Endpoint for main chat processing
export LLM_ENDPOINT=http://ollama:11434/api/generate
export DEFAULT_MODEL=deepseek-r1:1.5b

# Summarization service endpoint
export SUMMARIZATION_BASE_URL=http://ollama:11434/api/generate
export SUMMARIZATION_MODEL=llama3.2:1b
export SUMMARIZATION_TIMEOUT_SECONDS=60
export SUMMARIZATION_MAX_LENGTH=2000
```

### 3. Test Configuration (test/appsettings.Test.json)

Test-specific configuration with additional Ollama options:

```json
{
  "Ollama": {
    "BaseUrl": "http://ollama:11434",
    "GenerateEndpoint": "http://ollama:11434/api/generate",
    "ChatEndpoint": "http://ollama:11434/api/chat",
    "DefaultModel": "deepseek-r1:1.5b",
    "Stream": false,
    "TimeoutSeconds": 120
  }
}
```

## How Configuration is Used

### Program.cs (Main Application)

The configuration is loaded from appsettings.json and environment variables:

```csharp
// Configure ExecutorOptions
builder.Services.Configure<ExecutorOptions>(options =>
{
    options.LlmEndpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT") 
        ?? "http://ollama:11434/api/generate";
    options.DefaultModel = Environment.GetEnvironmentVariable("DEFAULT_MODEL") 
        ?? "deepseek-r1:1.5b";
});

// Configure SummarizationOptions
builder.Services.AddSingleton<SummarizationOptions>(provider =>
{
    var options = new SummarizationOptions();
    provider.GetRequiredService<IConfiguration>().GetSection("Summarization").Bind(options);
    
    // Override with environment variables
    options.BaseUrl = Environment.GetEnvironmentVariable("SUMMARIZATION_BASE_URL") ?? options.BaseUrl;
    options.Model = Environment.GetEnvironmentVariable("SUMMARIZATION_MODEL") ?? options.Model;
    
    return options;
});
```

### Services Usage

#### OllamaLlmClient

The `OllamaLlmClient` service uses the configured endpoint:

```csharp
public async Task<string> SendAsync(OllamaRequest request, string endpoint, CancellationToken cancellationToken = default)
{
    // Uses the endpoint parameter passed from configuration
    var baseUri = GetBaseUri(endpoint);
    // ...
}
```

#### LlmChatService

The `LlmChatService` automatically routes to Ollama based on endpoint detection:

```csharp
public async Task<string> SendChatRequestAsync(OllamaRequest request, AIModel? aiModel, CancellationToken cancellationToken = default)
{
    var endpoint = aiModel?.EndpointAddress ?? _executorOptions.LlmEndpoint;
    
    if (OllamaLlmClient.LooksLikeOllamaEndpoint(endpoint))
    {
        return await _ollamaClient.SendAsync(request, endpoint, cancellationToken);
    }
    // ...
}
```

## Configuration Hierarchy

The application follows this configuration priority (highest to lowest):

1. **Environment Variables** - Highest priority
2. **appsettings.{Environment}.json** - Environment-specific
3. **appsettings.json** - Base configuration
4. **Default values in code** - Fallback

## Docker Deployment

When running in Docker, set environment variables in docker-compose.yml or .env file:

```yaml
environment:
  - LLM_ENDPOINT=http://ollama:11434/api/generate
  - DEFAULT_MODEL=deepseek-r1:1.5b
  - SUMMARIZATION_BASE_URL=http://ollama:11434/api/generate
  - SUMMARIZATION_MODEL=llama3.2:1b
```

Or in `/home/runner_user/envs/backend.env`:

```env
LLM_ENDPOINT=http://ollama:11434/api/generate
DEFAULT_MODEL=deepseek-r1:1.5b
SUMMARIZATION_BASE_URL=http://ollama:11434/api/generate
SUMMARIZATION_MODEL=llama3.2:1b
```

## Changing Ollama Endpoint

To use a different Ollama instance:

### Option 1: Update appsettings.json

```json
{
  "Executor": {
    "LlmEndpoint": "http://custom-ollama-host:11434/api/generate"
  },
  "Summarization": {
    "BaseUrl": "http://custom-ollama-host:11434/api/generate"
  }
}
```

### Option 2: Set Environment Variables

```bash
export LLM_ENDPOINT=http://custom-ollama-host:11434/api/generate
export SUMMARIZATION_BASE_URL=http://custom-ollama-host:11434/api/generate
```

### Option 3: Command Line (dotnet run)

```bash
dotnet run --LLM_ENDPOINT=http://custom-ollama-host:11434/api/generate
```

## Supported Endpoints

The application supports both Ollama API endpoints:

1. **Generate API** (`/api/generate`): For prompt-based generation
2. **Chat API** (`/api/chat`): For conversation-style chat with message history

The `OllamaLlmClient` automatically detects which endpoint to use based on the request structure.

## Verifying Configuration

Check the logs at startup to see the active configuration:

```
BackendExecutor starting with configuration:
  Redis Connection: nodpt-redis:6379
  LLM Endpoint: http://ollama:11434/api/generate
  Default Model: deepseek-r1:1.5b
  Summarization Base URL: http://ollama:11434/api/generate
  Summarization Model: llama3.2:1b
```

## Testing Configuration

Use the test project to verify your Ollama configuration:

```bash
cd Executor/test
dotnet test --filter "Test_OllamaConnectivity_WithStreamFalse"
```

This will test connectivity to your configured Ollama endpoint.
