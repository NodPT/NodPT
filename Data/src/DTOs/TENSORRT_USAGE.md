# TensorRT-LLM Support in Ollama Payload Classes

This document provides usage examples for the TensorRT-LLM extensions added to the Ollama payload classes.

## Overview

The payload classes have been extended to support both Ollama and TensorRT-LLM backends. All new properties are optional and backward compatible with existing Ollama usage.

## Basic Usage (Ollama Compatible)

```csharp
// Standard Ollama request - still works as before
var request = new OllamaRequest
{
    model = "llama2",
    messages = new List<OllamaMessage>
    {
        new OllamaMessage { role = "user", content = "Hello!" }
    },
    options = new OllamaOptions
    {
        Temperature = 0.7,
        NumPredict = 2048,
        TopK = 40,
        TopP = 0.9
    }
};
```

## TensorRT-LLM Extensions

### 1. Frequency and Presence Penalties

```csharp
var options = new OllamaOptions
{
    Temperature = 0.7,
    frequency_penalty = 0.5,  // Penalize frequently used tokens
    presence_penalty = 0.3,   // Penalize tokens that appeared
    logprobs = true          // Return log probabilities
};
```

### 2. Response Format - JSON Object Mode

Force output to be valid JSON without schema enforcement:

```csharp
var request = new OllamaRequest
{
    model = "tensorrt-llm-model",
    messages = new List<OllamaMessage>
    {
        new OllamaMessage { role = "user", content = "Return user info as JSON" }
    },
    response_format = new ResponseFormat
    {
        type = "json_object"
    }
};
```

### 3. Response Format - JSON Schema Mode

Enforce a specific JSON schema for structured output:

```csharp
var request = new OllamaRequest
{
    model = "tensorrt-llm-model",
    messages = new List<OllamaMessage>
    {
        new OllamaMessage { role = "user", content = "Get user information" }
    },
    response_format = new ResponseFormat
    {
        type = "json_schema",
        schema = new JsonSchema
        {
            type = "object",
            properties = new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchema { type = "string" },
                ["age"] = new JsonSchema { type = "number", minimum = 0 },
                ["email"] = new JsonSchema { type = "string", pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$" },
                ["role"] = new JsonSchema 
                { 
                    type = "string",
                    @enum = new List<object> { "admin", "user", "guest" }
                }
            },
            required = new List<string> { "name", "email" }
        }
    }
};
```

### 4. Complex Nested Schema

```csharp
var schema = new JsonSchema
{
    type = "object",
    properties = new Dictionary<string, JsonSchema>
    {
        ["user"] = new JsonSchema
        {
            type = "object",
            properties = new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchema { type = "string" },
                ["contacts"] = new JsonSchema
                {
                    type = "array",
                    items = new JsonSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, JsonSchema>
                        {
                            ["type"] = new JsonSchema { type = "string" },
                            ["value"] = new JsonSchema { type = "string" }
                        }
                    }
                }
            }
        }
    }
};

var request = new OllamaRequest
{
    model = "tensorrt-llm-model",
    messages = new List<OllamaMessage>
    {
        new OllamaMessage { role = "user", content = "Get user with contacts" }
    },
    response_format = new ResponseFormat
    {
        type = "json_schema",
        schema = schema
    }
};
```

### 5. Function Calling with Tools

```csharp
var request = new OllamaRequest
{
    model = "tensorrt-llm-model",
    messages = new List<OllamaMessage>
    {
        new OllamaMessage { role = "user", content = "What's the weather in Seattle?" }
    },
    tools = new List<Tool>
    {
        new Tool
        {
            type = "function",
            function = new ToolFunction
            {
                name = "get_weather",
                description = "Get the current weather for a location",
                parameters = new JsonSchema
                {
                    type = "object",
                    properties = new Dictionary<string, JsonSchema>
                    {
                        ["location"] = new JsonSchema 
                        { 
                            type = "string",
                            description = "The city and state, e.g. San Francisco, CA"
                        },
                        ["unit"] = new JsonSchema
                        {
                            type = "string",
                            @enum = new List<object> { "celsius", "fahrenheit" }
                        }
                    },
                    required = new List<string> { "location" }
                }
            }
        }
    },
    tool_choice = "auto"  // Can also be "none" or a specific tool name
};
```

### 6. Metadata Passthrough

```csharp
var options = new OllamaOptions
{
    Temperature = 0.7,
    metadata = new Dictionary<string, object>
    {
        ["request_id"] = "req-123456",
        ["user_id"] = "user-789",
        ["custom_parameter"] = 42
    }
};
```

## Property Reference

### OllamaOptions - TensorRT-LLM Extensions

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `frequency_penalty` | `double?` | Penalizes tokens based on frequency in text | 0.0 |
| `presence_penalty` | `double?` | Penalizes tokens that already appeared | 0.0 |
| `logprobs` | `bool?` | Return log probabilities per token | null |
| `metadata` | `Dictionary<string, object>?` | Passthrough metadata for pipeline | null |

### OllamaRequest - TensorRT-LLM Extensions

| Property | Type | Description |
|----------|------|-------------|
| `response_format` | `ResponseFormat?` | Structured output format configuration |
| `tools` | `List<Tool>?` | Function calling tool definitions |
| `tool_choice` | `object?` | Tool selection strategy ("auto", "none", or tool name) |

### ResponseFormat

| Property | Type | Description |
|----------|------|-------------|
| `type` | `string?` | "json_object" or "json_schema" |
| `schema` | `JsonSchema?` | JSON Schema definition (for json_schema mode) |

### JsonSchema

Supports standard JSON Schema properties including:
- `type`, `properties`, `required`
- `items` (for arrays)
- `enum` (for constrained values)
- `minimum`, `maximum` (for numbers)
- `minLength`, `maxLength` (for strings/arrays)
- `pattern` (regex for strings)
- `description`, `title`, `default`
- `additionalProperties`

## Backward Compatibility

All new properties are:
- Optional (nullable)
- Ignored when serializing null values (`JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)`)
- Fully backward compatible with existing Ollama usage

Existing code continues to work without modifications.
