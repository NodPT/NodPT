# Executor Tests

This folder contains integration tests for the NodPT Executor service.

## Overview

The tests validate the complete workflow of the Executor application including:
- Redis message queuing
- Ollama LLM integration
- Chat message processing
- Summarization functionality
- Stream management

## Test Structure

### ExecutorIntegrationTests

Contains 7 comprehensive tests:

1. **Test_HelloMessage_AddedToRedis**: Validates that a simple "hello" message can be added to Redis
2. **Test_DummySongComposerMessages_AddedToRedis**: Adds 10 dummy song composer messages to test workflow
3. **Test_OllamaConnectivity_WithStreamFalse**: Tests Ollama connectivity with stream=false for quick responses
4. **Test_OllamaRequest_WithFormatForDirectorNode**: Tests format enforcement for Director node JSON output
5. **Test_ChatHistoryWithMessages**: Tests using messages array for chat history management
6. **Test_SummarizationFlow**: Tests the summarization functionality
7. **Test_StreamInfo**: Inspects Redis stream information and messages

## Configuration

Tests use `appsettings.Test.json` which configures:
- Ollama endpoint (default: http://ollama:11434)
- Redis connection (default: localhost:6379)
- Test-specific stream keys (jobs:chat:test, signalr:updates:test)
- Model settings
- Summarization options

### Configurable Ollama Endpoint

The Ollama endpoint can be configured in multiple ways:

1. **appsettings.Test.json**:
```json
{
  "Ollama": {
    "BaseUrl": "http://ollama:11434",
    "GenerateEndpoint": "http://ollama:11434/api/generate",
    "DefaultModel": "deepseek-r1:1.5b"
  }
}
```

2. **Environment Variables**:
```bash
export Ollama__BaseUrl=http://ollama:11434
export Ollama__GenerateEndpoint=http://ollama:11434/api/generate
```

## Running Tests

### Prerequisites

1. Redis must be running and accessible
2. Ollama must be running with the required models installed
3. .NET 8 SDK installed

### Run All Tests

```bash
cd Executor/test
dotnet test
```

### Run with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test

```bash
dotnet test --filter "Test_HelloMessage_AddedToRedis"
```

## Test Output

All tests print detailed log information to the console including:
- Configuration values
- Request payloads (JSON)
- Response bodies
- Entry IDs
- Stream information
- Success/failure indicators

Example output:
```
=== TEST 1: Add Hello Message to Redis ===
✓ Hello message added to Redis stream
  Entry ID: 1234567890000-0
  Stream Key: jobs:chat:test
  Message: Hello! This is a test message.
  Current stream length: 1
```

## Key Features

### Stream=False for Quick Testing

All Ollama requests use `stream=false` to get synchronous responses, making tests faster and easier to debug.

### Messages Array for Chat History

Tests use the `messages` array format instead of `prompt` to properly test chat history functionality:

```json
{
  "model": "deepseek-r1:1.5b",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "Hello"}
  ],
  "stream": false
}
```

### Format Enforcement

Tests include format enforcement for Director node output using the `format` field:

```json
{
  "model": "deepseek-r1:1.5b",
  "messages": [...],
  "stream": false,
  "format": "json"
}
```

## Cleanup

Test streams are preserved after test runs for manual inspection. To clean up:

```bash
redis-cli
> DEL jobs:chat:test
> DEL signalr:updates:test
```

## Troubleshooting

### Redis Connection Issues

If tests fail with Redis connection errors:
1. Verify Redis is running: `redis-cli ping`
2. Check connection string in appsettings.Test.json
3. Ensure Redis port 6379 is accessible

### Ollama Connection Issues

If tests fail with Ollama connectivity errors:
1. Verify Ollama is running: `curl http://ollama:11434/api/tags`
2. Check endpoint configuration in appsettings.Test.json
3. Ensure required models are installed: `ollama list`
4. Pull missing models: `ollama pull deepseek-r1:1.5b`

### Model Not Found

If tests fail with model not found:
```bash
# Pull the required models
ollama pull deepseek-r1:1.5b
ollama pull llama3.2:1b
```

## Integration with Executor

These tests validate the inputs that the Executor service will process. The Executor service:
1. Listens to `jobs:chat` stream
2. Processes messages with ChatStreamWorker
3. Calls Ollama API with messages array
4. Handles summarization for long conversations
5. Publishes results to `signalr:updates` stream

## CI/CD Integration

Tests can be integrated into CI/CD pipelines:

```bash
# In GitHub Actions or similar
- name: Run Executor Tests
  run: |
    cd Executor/test
    dotnet test --logger "console;verbosity=detailed"
```

## Contributing

When adding new tests:
1. Follow the existing naming pattern: `Test_DescriptiveName`
2. Use ITestOutputHelper for detailed logging
3. Include success/failure indicators (✓/✗)
4. Document the test purpose in comments
5. Clean up resources in Dispose() if needed
