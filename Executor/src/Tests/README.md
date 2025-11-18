# Executor Tests

This directory contains test utilities for the Executor service.

## TestLlmClient

A simple test utility to verify the LlmChatService works correctly with a real or mock LLM endpoint.

### Usage

```bash
# Test with default endpoint (http://localhost:8355/v1/chat/completions)
dotnet run --project TestLlmClient.csproj

# Test with custom endpoint
dotnet run --project TestLlmClient.csproj http://custom-llm:8000/v1/chat/completions
```

### Requirements

- A running LLM endpoint (e.g., TensorRT-LLM) at the specified URL
- Or a mock server that responds to the OpenAI-compatible chat completions format

### Expected Behavior

The test will:
1. Send a simple string message to the LLM
2. Send an object message (serialized) to the LLM
3. Display the responses received
4. Report any errors encountered
