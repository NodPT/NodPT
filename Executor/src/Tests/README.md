# Executor Tests

This directory contains test utilities for the Executor service.

## TestLlmClient

A simple test utility to verify the LlmChatService works correctly with a real or mock LLM endpoint.

### Usage

This test utility is included in the main BackendExecutor project. To run it, you need to temporarily comment out the main Program.cs entry point or extract this test to a separate console project.

**Option 1: Create a standalone test project**
```bash
# Create a new console project for testing
dotnet new console -n LlmChatServiceTest -o Tests/LlmChatServiceTest
# Copy TestLlmClient.cs to the new project and reference BackendExecutor
# Then run with:
cd Tests/LlmChatServiceTest
dotnet run

# With custom endpoint:
dotnet run http://custom-llm:8000/v1/chat/completions
```

**Option 2: Use the service directly in your code**
```csharp
var llmService = serviceProvider.GetRequiredService<ILlmChatService>();
await llmService.SendChatMessageAsync("test message", "trt-llm-manager", 64);
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
