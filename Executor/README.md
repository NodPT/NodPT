# NodPT Executor

Background worker service built with .NET 8 that processes jobs from Redis streams and executes AI-powered tasks. The Executor is the core processing engine that orchestrates workflow execution and AI interactions. **Now uses shared RedisService from NodPT.Data project.**

## 🛠️ Technology Stack

- **.NET 8.0**: Modern .NET framework for background services
- **Worker Service**: Long-running background service template
- **Redis Pub/Sub & Lists**: Message queuing and pub/sub communication
- **StackExchange.Redis**: Redis client library (v2.9.32)
- **NodPT.Data**: Shared data layer with RedisService
- **HTTP Client**: Communication with AI services (Ollama)
- **System.Text.Json**: JSON serialization

### Key Features

- Role-based job execution (Manager, Inspector, Agent)
- Chat job processing with AI integration
- Redis pub/sub for AI request/response communication
- Shared RedisService for consistent Redis operations
- LLM chat integration
- Real-time notifications to WebAPI via Redis channels
- Docker ready

## 🏗️ Architecture

### Chat Job Processing Flow

```
WebAPI /api/chat/send
     │
     ▼
Redis List (chat.jobs) ← AI.REQUEST
     │
     ▼
ChatJobConsumer
     │
     ▼
LLM Service (Ollama)
     │
     ▼
Redis Pub/Sub (AI.RESPONSE) ← AI.RESPONSE
     │
     ▼
WebAPI RedisAIResponseListener
     │
     ▼
SignalR Client (ReceiveAIResponse)
```

### Traditional Job Processing Flow

```
Redis Stream (jobs:manager/inspector/agent)
    │
    ▼
RedisConsumer
    │
    ├─→ Manager Runner ──→ LLM (trt-llm-manager)
    ├─→ Inspector Runner ──→ LLM (trt-llm-inspector)
    └─→ Agent Runner ──→ LLM (trt-llm-agent)
    │
    ▼
Process Result
    │
    ├─→ Save to Repository
    └─→ Notify via Redis stream (signalr:updates)
```

### Project Structure

```
Executor/
├── src/
│   ├── Config/            # Configuration classes
│   ├── Consumers/         # Redis Streams consumers
│   │   ├── JobConsumer.cs        # Job stream consumer
│   │   └── ChatConsumer.cs       # Chat stream consumer
│   ├── Data/              # Data structures and interfaces
│   │   ├── IJobRepository.cs     # Repository interface
│   │   └── JobMessage.cs         # Job data model
│   ├── Dispatch/          # Job dispatcher
│   │   └── JobDispatcher.cs      # Concurrency control
│   ├── Notify/            # Notification interfaces
│   │   └── ISignalRNotifier.cs   # SignalR notification
│   ├── Runners/           # Job execution runners
│   │   ├── ManagerRunner.cs      # Manager job runner
│   │   ├── InspectorRunner.cs    # Inspector job runner
│   │   └── AgentRunner.cs        # Agent job runner
│   ├── Services/          # External services
│   │   └── LlmChatService.cs     # LLM communication
│   ├── Program.cs         # Application entry point
│   ├── Worker.cs          # Background worker for jobs
│   ├── ChatWorker.cs      # Background worker for chat
│   └── BackendExecutor.csproj    # Project file
├── Dockerfile             # Docker container config
├── docker-compose.yml     # Docker Compose config
└── README.md             # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Redis server (with Streams support)
- AI service (Ollama or compatible LLM endpoint)
- Docker (for containerized deployment)

### Local Development

1. **Install .NET 8 SDK**:
   Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

2. **Navigate to project**:
   ```bash
   cd Executor/src
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Configure appsettings.Development.json or set environment variables**:
    
    **Option 1: Using environment variables** (recommended):
    ```bash
    export DB_HOST=localhost
    export DB_PORT=3306
    export DB_NAME=nodpt
    export DB_USER=your_user
    export DB_PASSWORD=your_password
    export REDIS_CONNECTION=localhost:6379
    export LLM_ENDPOINT=http://localhost:11434/v1/chat/completions
    ```
    
    **Option 2: Using appsettings.Development.json**:
    ```json
    {
      "Redis": {
        "ConnectionString": "localhost:6379"
      },
      "Concurrency": {
        "MaxManager": 5,
        "MaxInspector": 10,
        "MaxAgent": 20,
        "MaxTotal": 50
      },
      "LLM": {
        "Endpoint": "http://localhost:11434/v1/chat/completions"
      }
    }
    ```
    
    Note: Database configuration must be set via environment variables as shown in Option 1.

5. **Run the application**:
   ```bash
   dotnet run
   ```

### Build for Production

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

## 🐳 Docker Deployment

### Environment Setup

Create or update environment file at `/home/runner_user/envs/backend.env`:

```env
# Database Configuration
DB_HOST=your_db_host_here
DB_PORT=your_db_port_here
DB_NAME=your_db_name_here
DB_USER=your_db_user_here
DB_PASSWORD=your_db_password_here

# Redis Configuration
REDIS_CONNECTION=nodpt-redis:6379

# Concurrency Limits (0 = unlimited)
MAX_MANAGER=5
MAX_INSPECTOR=10
MAX_AGENT=20
MAX_TOTAL=50

# LLM Configuration
LLM_ENDPOINT=http://ollama:11434/v1/chat/completions

# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
```

### Build and Run with Docker

```bash
# Create network if not exists
docker network create backend_network

# Build the image (from repository root)
cd Executor
docker-compose build

# Start the container
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

### Dockerfile

Multi-stage build optimized for .NET 8:

1. **Build Stage**: Builds the application
2. **Publish Stage**: Creates release package
3. **Runtime Stage**: Uses .NET runtime (lightweight)

## ⚙️ Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DB_HOST` | - | Database host (required) |
| `DB_PORT` | - | Database port (required) |
| `DB_NAME` | - | Database name (required) |
| `DB_USER` | - | Database username (required) |
| `DB_PASSWORD` | - | Database password (required) |
| `REDIS_CONNECTION` | `localhost:6379` | Redis connection string |
| `MAX_MANAGER` | `0` | Max concurrent manager jobs (0 = unlimited) |
| `MAX_INSPECTOR` | `0` | Max concurrent inspector jobs (0 = unlimited) |
| `MAX_AGENT` | `0` | Max concurrent agent jobs (0 = unlimited) |
| `MAX_TOTAL` | `0` | Max total concurrent jobs (0 = unlimited) |
| `LLM_ENDPOINT` | `http://localhost:11434/v1/chat/completions` | LLM API endpoint |

### Concurrency Control

The dispatcher ensures jobs don't exceed configured limits:

- Per-role limits (Manager, Inspector, Agent)
- Total concurrent job limit
- Automatic queuing when at capacity

## 📨 Redis Communication

The Executor uses Redis for bidirectional communication with WebAPI:

### Consuming Messages (Input)

**1. Chat Jobs (Redis List: `chat.jobs`)**
```json
{
  "UserId": "user-firebase-uid",
  "ConnectionId": "signalr-connection-id",
  "NodeId": "node-id",
  "ProjectId": "project-id",
  "Message": "user message",
  "Model": "model-name",
  "ChatMessageId": "message-id"
}
```

**2. Workflow Jobs (Redis Streams)**
- `jobs:manager`: Manager-level jobs (high-level planning)
- `jobs:inspector`: Inspector-level jobs (code review, analysis)
- `jobs:agent`: Agent-level jobs (specific tasks)

```json
{
  "jobId": "unique-job-id",
  "workflowId": "workflow-id",
  "userId": "user-id",
  "projectId": "project-id",
  "connectionId": "signalr-connection-id",
  "task": "task-description",
  "payload": "{\"key\":\"value\"}"
}
```

### Publishing Messages (Output)

**1. AI Responses (Redis Pub/Sub: `AI.RESPONSE`)**
```json
{
  "ConnectionId": "signalr-connection-id",
  "Content": "AI response text"
}
```

**2. Workflow Updates (Redis Stream: `signalr:updates`)**
```json
{
  "MessageId": "msg-id",
  "NodeId": "node-id",
  "ProjectId": "project-id",
  "UserId": "user-id",
  "Type": "result",
  "Payload": "result data",
  "Timestamp": "2024-01-01T00:00:00Z"
}
```

### Using IRedisService

The Executor uses the shared `IRedisService` from NodPT.Data:

```csharp
// Inject IRedisService
private readonly IRedisService _redisService;

// Consume from list
var message = await _redisService.ListLeftPopAsync("chat.jobs");

// Publish to channel
await _redisService.PublishAsync("AI.RESPONSE", responseJson);
```

## 🤖 LLM Integration

### LLM Chat Service

The executor includes an `LlmChatService` for AI interactions.

#### Send String Message

```csharp
private readonly ILlmChatService _llmChatService;

var response = await _llmChatService.SendChatMessageAsync(
    message: "Explain this code",
    model: "trt-llm-manager",
    maxTokens: 128,
    cancellationToken: cancellationToken
);
```

#### Send Object Message

```csharp
var messageObject = new
{
    prompt = "Analyze this workflow",
    context = "Node-based editor",
    requirements = new[] { "performance", "security" }
};

var response = await _llmChatService.SendChatMessageAsync(
    messageObject: messageObject,
    model: "trt-llm-inspector",
    maxTokens: 256,
    cancellationToken: cancellationToken
);
```

### LLM API Format

Request format (OpenAI-compatible):

```json
{
  "model": "model-name",
  "messages": [
    {
      "role": "user",
      "content": "message-content"
    }
  ],
  "max_tokens": 128
}
```

Response format:

```json
{
  "choices": [
    {
      "message": {
        "content": "AI response text"
      }
    }
  ]
}
```

### Supported Models

- `trt-llm-manager`: Manager-level reasoning
- `trt-llm-inspector`: Code inspection and analysis
- `trt-llm-agent`: Task execution

## 🔄 Job Execution Flow

1. **Consumer reads from Redis Stream**: `jobs:{role}` stream
2. **Dispatcher checks concurrency**: Ensure limits not exceeded
3. **Runner processes job**:
   - Extract job data
   - Call LLM service if needed
   - Process results
4. **Save results**: Store in repository (database)
5. **Notify frontend**: Send results to `signalr:updates` stream
6. **Acknowledge job**: Mark as processed in Redis

### Consumer Groups

Redis consumer groups ensure reliable processing:

- Group name: `executor-group`
- Consumer name: Unique per instance
- Auto-creation of groups on startup

## 📊 Job Runners

### Manager Runner

High-level planning and orchestration:

```csharp
public async Task<string> ExecuteAsync(JobMessage job, CancellationToken ct)
{
    // Use LLM for planning
    var response = await _llmService.SendChatMessageAsync(
        message: job.Task,
        model: "trt-llm-manager",
        maxTokens: 256,
        cancellationToken: ct
    );
    
    return response;
}
```

### Inspector Runner

Code review and analysis:

```csharp
public async Task<string> ExecuteAsync(JobMessage job, CancellationToken ct)
{
    // Use LLM for inspection
    var response = await _llmService.SendChatMessageAsync(
        messageObject: new { code = job.Payload, task = job.Task },
        model: "trt-llm-inspector",
        maxTokens: 512,
        cancellationToken: ct
    );
    
    return response;
}
```

### Agent Runner

Specific task execution:

```csharp
public async Task<string> ExecuteAsync(JobMessage job, CancellationToken ct)
{
    // Use LLM for task execution
    var response = await _llmService.SendChatMessageAsync(
        message: job.Task,
        model: "trt-llm-agent",
        maxTokens: 128,
        cancellationToken: ct
    );
    
    return response;
}
```

## 🔔 SignalR Notifications

Results are sent to SignalR via Redis stream:

```csharp
// Write to signalr:updates stream
await _redis.GetDatabase().StreamAddAsync("signalr:updates", new[]
{
    new NameValueEntry("MessageId", messageId),
    new NameValueEntry("NodeId", nodeId),
    new NameValueEntry("ProjectId", projectId),
    new NameValueEntry("UserId", userId),
    new NameValueEntry("Type", "result"),
    new NameValueEntry("Payload", result),
    new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("o"))
});
```

SignalR service reads this stream and delivers to connected clients.

## 🧪 Testing

```bash
# Run tests (if available)
dotnet test

# Test Redis connection
redis-cli -h localhost -p 6379 ping

# Test LLM endpoint
curl -X POST http://localhost:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"llama2","messages":[{"role":"user","content":"test"}]}'
```

## 🤝 Contributing

### Development Guidelines

1. Use async/await for all I/O operations
2. Implement proper cancellation token handling
3. Use structured logging with proper log levels
4. Handle exceptions gracefully
5. Write unit tests for runners and services
6. Document complex logic with comments

### Adding New Job Runners

1. Create runner class implementing `IJobRunner`
2. Register in dependency injection (Program.cs)
3. Add corresponding Redis stream
4. Configure concurrency limits
5. Update documentation

### Code Style

- Follow .NET naming conventions
- Use dependency injection
- Keep runners focused and single-purpose
- Use configuration for external dependencies

## 🐛 Troubleshooting

### Common Issues

**Redis connection fails**:
```bash
# Verify Redis is running
docker ps | grep redis

# Test connection
redis-cli -h nodpt-redis -p 6379 ping
```

**LLM endpoint not responding**:
```bash
# Check Ollama service
docker ps | grep ollama

# Test endpoint
curl http://ollama:11434/api/tags
```

**Jobs not being processed**:
- Check Redis streams: `redis-cli XINFO STREAM jobs:manager`
- Verify consumer group exists
- Check executor logs: `docker-compose logs -f`
- Ensure concurrency limits not too restrictive

**Out of memory errors**:
- Reduce concurrency limits
- Check LLM max_tokens settings
- Monitor container resource usage

## 📈 Monitoring

### Logs

```bash
# Docker logs
docker-compose logs -f nodpt-executor

# .NET logs location (if running locally)
./logs/executor-{date}.log
```

### Metrics to Monitor

- Job processing rate
- Job queue depth (Redis stream length)
- Concurrent job count
- LLM response times
- Error rates
- Memory usage

## 🔒 Security

- Never log sensitive job payloads
- Validate all job data before processing
- Use secure connections to Redis and LLM
- Implement rate limiting if exposed publicly
- Regular security updates for dependencies

## 📚 Dependencies

This project depends on:
- **NodPT.Data**: Shared data layer with RedisService, models, and services
- **Redis**: Message streaming, pub/sub, and job queuing
- **AI Service**: LLM endpoint (Ollama or compatible)

### Key Project References

- `NodPT.Data.csproj`: Provides IRedisService and shared models
- `StackExchange.Redis` (2.9.32): Redis client library

### Redis Communication Flow

```
WebAPI ←─[AI.RESPONSE]──→ Executor
   │                          │
   └──[chat.jobs list]───────→│
   │                          │
   ←──[signalr:updates]───────┘
```

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check executor logs for errors
- Verify Redis and AI services are running
- Contact the development team