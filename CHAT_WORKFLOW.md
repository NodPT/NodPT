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
  "NodeLevel": "manager"
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
1. Saves message to database via `ChatService`
2. Pushes job to Redis list `chat.jobs`
3. Returns immediately with "queued" status

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
- `Executor/src/ChatWorker.cs` - Background worker for chat jobs

**Background Worker**: Runs continuously to process chat jobs

**Process**:
1. Pops messages from Redis list `chat.jobs` (FIFO)
2. Extracts model name from `NodeLevel`
3. Processes message with AI (currently simulated, ready for TensorRT-LLM)
4. Publishes response to Redis channel `AI.RESPONSE`

**Model Mapping**:
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

### TensorRT-LLM Integration

Replace the simulation in `ChatJobConsumer.cs` with actual TensorRT-LLM calls:

```csharp
private async Task<string> CallTensorRtLlm(ChatJobDto chatJob, string modelName, CancellationToken cancellationToken)
{
    // TODO: Implement actual TensorRT-LLM HTTP call
    var client = new HttpClient();
    var request = new
    {
        model = modelName,
        prompt = chatJob.Message,
        user_id = chatJob.UserId,
        project_id = chatJob.ProjectId
    };
    
    var response = await client.PostAsJsonAsync("http://tensorrt-llm:8000/generate", request, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<TensorRtLlmResponse>(cancellationToken);
    return result.GeneratedText;
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
