# Backend Executor

A .NET 8 Worker Service that reads jobs from Redis Streams and executes them based on role (manager, inspector, agent).

## Features

- **Redis Streams Consumer**: Reads from `jobs:manager`, `jobs:inspector`, and `jobs:agent` streams
- **Role-based Execution**: Supports manager, inspector, and agent job types
- **Concurrency Control**: Configurable limits per role and total concurrent jobs
- **Repository Integration**: Saves job results (stubbed for now)
- **SignalR Notifications**: Sends completion notifications (stubbed for now)
- **Docker Ready**: Includes Dockerfile for containerization

## Configuration

Configuration can be set via environment variables or `appsettings.json`:

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `REDIS_CONNECTION` | `localhost:6379` | Redis connection string |
| `MAX_MANAGER` | `0` | Max concurrent manager jobs (0 = unlimited) |
| `MAX_INSPECTOR` | `0` | Max concurrent inspector jobs (0 = unlimited) |
| `MAX_AGENT` | `0` | Max concurrent agent jobs (0 = unlimited) |
| `MAX_TOTAL` | `0` | Max total concurrent jobs (0 = unlimited) |

## Project Structure

```
BackendExecutor/
├── Config/           # Configuration classes
├── Consumers/        # Redis Streams consumers
├── Data/            # Data structures and repository interfaces
├── Dispatch/        # Job dispatcher with concurrency control
├── Notify/          # SignalR notification interfaces
├── Runners/         # Job runners for each role
├── Program.cs       # Main application entry point
├── Worker.cs        # Background service worker
└── Dockerfile       # Container configuration
```

## Running the Application

### Local Development

```bash
dotnet run
```

### Docker

```bash
# Build image
docker build -t backend-executor .

# Run with default settings
docker run backend-executor

# Run with custom Redis connection
docker run -e REDIS_CONNECTION=redis:6379 backend-executor

# Run with concurrency limits
docker run -e MAX_MANAGER=5 -e MAX_INSPECTOR=10 -e MAX_AGENT=20 -e MAX_TOTAL=50 backend-executor
```

## Job Message Format

Jobs should be added to Redis Streams with the following fields:

```json
{
  "jobId": "unique-job-id",
  "workflowId": "workflow-id", 
  "connectionId": "signalr-connection-id",
  "task": "task-description",
  "payload": "{\"key\":\"value\"}"
}
```

The role is determined by the stream name (jobs:manager, jobs:inspector, jobs:agent).

## Implementation Notes

- Repository and Notifier interfaces are stubbed for demonstration
- Real implementations would save to database and send SignalR notifications
- Consumer groups are automatically created for Redis Streams
- Failed jobs are logged but not retried (add retry logic as needed)
- Jobs are acknowledged after successful processing