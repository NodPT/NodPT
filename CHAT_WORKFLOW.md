# DeepChat Plugin and Chat Workflow

## Overview

This implementation integrates the DeepChat UI component with a complete backend workflow for AI-powered chat functionality in NodPT.

## Architecture Flow

```
Frontend (DeepChat) 
  ↓ HTTP POST
WebAPI (/api/chat/submit)
  ↓ Save to DB + Push to Redis List
Redis Queue (chat.jobs)
  ↓ Pop from List
Executor (ChatJobConsumer)
  ↓ Process with AI
Redis Channel (AI.RESPONSE)
  ↓ Publish
SignalR (RedisAiListener)
  ↓ SendAsync
Frontend (ReceiveAIResponse)
```

## Components

### 1. Frontend (Vue + DeepChat)

**File**: `Frontend/src/src/components/DeepChatComponent.vue`

- Integrates DeepChat UI component
- Connects to SignalR for real-time updates
- Sends user messages to `/api/chat/submit`
- Receives AI responses via `ReceiveAIResponse` event
- Automatically includes:
  - `UserId`: Current user ID
  - `ConnectionId`: SignalR connection ID
  - `ProjectId`: Current project context
  - `NodeLevel`: Selected node level (manager/inspector/agent)

**Integration**: Used in `RightPanel.vue` as the AI Chat tab component

### 2. WebAPI (C# .NET)

**Files**:
- `WebAPI/src/Controllers/ChatController.cs` - Chat submission endpoint
- `WebAPI/src/Services/RedisService.cs` - Redis operations
- `Data/src/DTOs/ChatSubmitDto.cs` - Chat submission data transfer object

**Endpoint**: `POST /api/chat/submit`

**Request Body**:
```json
{
  "UserId": "user123",
  "ConnectionId": "signalr-connection-id",
  "Message": "User's chat message",
  "ProjectId": "project-guid",
  "NodeLevel": "node-id",
  "Model": "llama2"
}
```

**Response**:
```json
{
  "Status": "queued",
  "MessageId": "message-guid"
}
```

**Process**:
1. Retrieves the model name from the node's configuration:
   - First checks if the node has a direct AIModel assigned
   - Otherwise, uses the matching AIModel from the project's template (based on MessageType and Level)
2. Saves message to database via `ChatService`
3. Pushes job to Redis list `chat.jobs` with the model name included
4. Returns immediately with "queued" status

### 3. SignalR Service

**File**: `SignalR/src/Services/RedisAiListener.cs`

**Background Service**: Runs continuously to listen for AI responses

**Process**:
1. Subscribes to Redis channel `AI.RESPONSE`
2. Receives AI response with `ConnectionId` and `Content`
3. Routes response to specific client via SignalR
4. Sends `ReceiveAIResponse` event to frontend

**Message Format** (from Redis):
```json
{
  "ConnectionId": "signalr-connection-id",
  "Content": "AI generated response"
}
```

### 4. Executor Service

**Files**:
- `Executor/src/Consumers/ChatJobConsumer.cs` - Chat job processor
- `Executor/src/Services/LlmChatService.cs` - Ollama API client
- `Executor/src/ChatWorker.cs` - Background worker for chat jobs

**Background Worker**: Runs continuously to process chat jobs

**Process**:
1. Pops messages from Redis list `chat.jobs` (FIFO)
2. Uses model name from the Redis data (provided by WebAPI from node/template configuration)
3. Sends request to Ollama API with `stream: false` parameter
4. Publishes response to Redis channel `AI.RESPONSE`

**Ollama API Integration**:
- Endpoint: Configured in `appsettings.json` as `Executor:LlmEndpoint`
- Request format:
  ```json
  {
    "model": "model-name",
    "messages": [{"role": "user", "content": "message"}],
    "stream": false
  }
  ```
- Response format:
  ```json
  {
    "message": {
      "role": "assistant",
      "content": "AI response"
    }
  }
  ```

**Fallback Model Mapping** (if Model field is not in Redis data):
- `manager` → `trt-llm-manager`
- `inspector` → `trt-llm-inspector`
- `agent` → `trt-llm-agent`
- Other → Uses as-is

## Configuration

### Environment Variables

**WebAPI**:
```env
REDIS_CONNECTION=localhost:6379
```

**SignalR**:
```env
Redis:ConnectionString=localhost:6379
```

**Executor**:
```env
REDIS_CONNECTION=localhost:6379
```

**Frontend**:
```env
VITE_API_BASE_URL=http://localhost:5015
VITE_SIGNALR_BASE_URL=http://localhost:8446
```

## Dependencies

### Frontend
- `deep-chat` - Chat UI component
- `@microsoft/signalr` - SignalR client

### Backend (All C# projects)
- `StackExchange.Redis` - Redis client

## Testing

### Manual Testing Steps

1. **Start Redis**:
   ```bash
   docker run -d -p 6379:6379 redis:latest
   ```

2. **Start WebAPI**:
   ```bash
   cd WebAPI/src
   dotnet run
   ```

3. **Start SignalR**:
   ```bash
   cd SignalR/src
   dotnet run
   ```

4. **Start Executor**:
   ```bash
   cd Executor/src
   dotnet run
   ```

5. **Start Frontend**:
   ```bash
   cd Frontend/src
   npm run dev
   ```

6. **Test the Flow**:
   - Open browser to frontend URL
   - Sign in
   - Open a project
   - Navigate to AI Chat tab
   - Send a message
   - Observe AI response appears after ~2 seconds

### Monitoring Redis

```bash
# Monitor Redis commands
redis-cli MONITOR

# Check queue length
redis-cli LLEN chat.jobs

# Check channel subscriptions
redis-cli PUBSUB CHANNELS
```

## Next Steps

### Model Configuration

To use different AI models:

1. **Create AIModel records** in the Template:
   - Set `ModelIdentifier` to the Ollama model name (e.g., "llama2", "mistral", "codellama")
   - Set `MessageType` and `Level` to match your workflow nodes

2. **Assign AIModel to Nodes**:
   - Either assign directly to a node via `Node.AIModel`
   - Or let nodes use the matching model from the template based on their `MessageType` and `Level`

3. **Configure Ollama endpoint**:
   - Update `Executor:LlmEndpoint` in `appsettings.json`
   - Default: `http://localhost:11434/api/chat` (Ollama's default endpoint)

### TensorRT-LLM Integration

The system now uses Ollama API format with `stream: false`. To integrate with TensorRT-LLM:

1. Configure TensorRT-LLM to expose an Ollama-compatible API endpoint
2. Update `Executor:LlmEndpoint` in `appsettings.json` to point to TensorRT-LLM
3. Ensure TensorRT-LLM accepts the same request format:
   ```json
   {
     "model": "model-name",
     "messages": [{"role": "user", "content": "message"}],
     "stream": false
   }
   ```

### Enhancements

1. **Error Handling**: Add retry logic for failed AI requests
2. **Monitoring**: Add metrics and logging for message throughput
3. **Persistence**: Save AI responses to database
4. **Rate Limiting**: Add rate limiting per user
5. **Message History**: Load previous messages on component mount
6. **Typing Indicators**: Show when AI is processing
7. **Message Reactions**: Allow users to rate AI responses

## Troubleshooting

### No AI Response Received

1. Check Redis is running: `redis-cli ping`
2. Check Executor logs for job processing
3. Check SignalR logs for channel subscription
4. Verify SignalR connection in browser console
5. Check Redis queue: `redis-cli LLEN chat.jobs`

### Messages Not Queued

1. Check WebAPI logs for errors
2. Verify Redis connection in WebAPI
3. Check `/api/chat/submit` endpoint is reachable

### SignalR Not Connected

1. Check CORS configuration
2. Verify Firebase authentication token
3. Check SignalR hub URL in frontend config
4. Review browser console for connection errors

## License

Same as parent project.
