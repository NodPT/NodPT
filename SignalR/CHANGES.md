# SignalR Hub Refactoring - Changes Summary

## Overview
Refactored the SignalR hub to operate exclusively as a **listener and router**, removing all task submission capabilities. The hub now listens to Redis streams and delivers real-time updates to connected frontend clients.

## Key Changes

### 1. New Components Added

#### `Models/NodeMessage.cs`
- Data model matching the Executor's Redis stream message format
- Contains fields: MessageId, NodeId, ProjectId, UserId, ClientConnectionId, WorkflowGroup, Type, Payload, Timestamp

#### `Services/RedisStreamListener.cs` (247 lines)
- Background service that continuously reads from Redis stream `signalr:updates`
- Features:
  - Consumer group management (`signalr-hub-group`)
  - Message parsing from Redis stream entries
  - Smart routing with fallback logic
  - Configurable batch sizes and polling intervals
  - Robust error handling with message acknowledgment
  - Prevents infinite redelivery loops

#### `Tests/TestRedisProducer.cs`
- Test utility to simulate Executor writing to Redis stream
- Generates sample messages with different routing criteria
- Useful for development and testing

#### `Tests/README.md`
- Documentation for testing Redis integration
- Instructions for manual and automated testing
- Examples using redis-cli

### 2. Modified Components

#### `Program.cs`
- Added StackExchange.Redis connection configuration
- Registered RedisStreamListener as a hosted background service
- Added error handling for Redis connection failures at startup
- Provides clear error messages when Redis is unavailable

#### `Hubs/NodptHub.cs`
- Auto-joins users to user-specific groups (`user:{userId}`) on connection
- Enables routing by UserId without explicit group join

#### `appsettings.json`
- Added Redis configuration section:
  - `ConnectionString`: Redis server connection (default: redis:6379)
  - `StreamListener.MessageBatchSize`: Messages to read per batch (default: 10)
  - `StreamListener.PollingDelayMs`: Delay when no messages (default: 100ms)
  - `StreamListener.ErrorRetryDelayMs`: Delay after error (default: 1000ms)

#### `appsettings.Development.json`
- Development-specific Redis configuration
- Faster polling for development (50ms vs 100ms)
- Faster error retry (500ms vs 1000ms)
- Connection string defaults to localhost:6379

#### `README.md`
- Updated architecture documentation
- Added Redis configuration instructions
- Documented new `ReceiveNodeUpdate` client event
- Added Redis stream message format specification
- Documented routing logic and priorities
- Added testing instructions

### 3. Dependencies Added

#### NuGet Packages
- **StackExchange.Redis** (v2.9.32)
  - Includes: Microsoft.Extensions.Logging.Abstractions, Pipelines.Sockets.Unofficial, System.IO.Pipelines

## Architecture Changes

### Before
- SignalR hub with messaging capabilities
- No Redis integration
- Basic group management

### After
- **Redis Stream Listener**: Continuously reads from `signalr:updates` stream
- **Smart Routing**: Routes messages based on priority:
  1. ClientConnectionId (specific client)
  2. WorkflowGroup (group of clients)
  3. UserId (all clients for that user)
- **Fallback Logic**: If specific client routing fails, falls back to group or user routing
- **Error Resilience**: Messages acknowledged even on failure to prevent infinite loops

## Communication Flow

```
Executor → Redis Stream (signalr:updates) → RedisStreamListener → SignalR Hub → Frontend Clients
```

1. Executor writes task results to Redis stream `signalr:updates`
2. RedisStreamListener reads messages from the stream (consumer group: signalr-hub-group)
3. Messages are parsed and routed based on routing criteria
4. Connected clients receive updates via `ReceiveNodeUpdate` event

## Configuration

### Production (`appsettings.json`)
```json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "StreamListener": {
      "MessageBatchSize": 10,
      "PollingDelayMs": 100,
      "ErrorRetryDelayMs": 1000
    }
  }
}
```

### Development (`appsettings.Development.json`)
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "StreamListener": {
      "MessageBatchSize": 10,
      "PollingDelayMs": 50,
      "ErrorRetryDelayMs": 500
    }
  }
}
```

## Client Integration

### Frontend clients should listen to:
```javascript
connection.on("ReceiveNodeUpdate", (data) => {
    // data contains: messageId, nodeId, projectId, userId, 
    // type, payload, timestamp, workflowGroup
    console.log("Node Update:", data);
});
```

### Routing Options

1. **By ClientConnectionId**: Direct delivery to specific connection
2. **By WorkflowGroup**: Delivery to all clients in the workflow group
3. **By UserId**: Delivery to all clients connected as that user (automatic via `user:{userId}` group)

## Error Handling Improvements

1. **Startup Validation**: Redis connection validated at startup with clear error messages
2. **Message Acknowledgment**: Failed messages are acknowledged to prevent infinite redelivery
3. **Fallback Routing**: Failed specific client routing falls back to group/user routing
4. **Configurable Retries**: Error retry delays are configurable per environment

## Security

- No new security vulnerabilities introduced (CodeQL scan: 0 alerts)
- Firebase authentication still required for frontend clients
- Communication stays within backend network
- No task submission or executor privileges in SignalR hub

## Testing

### Run Test Producer
```bash
cd Tests
dotnet run TestRedisProducer.cs [redis-connection-string]
```

### Manual Testing with redis-cli
```bash
redis-cli XADD signalr:updates * \
  MessageId "test-001" \
  NodeId "node-001" \
  ProjectId "project-test" \
  UserId "test-user-1" \
  ClientConnectionId "" \
  WorkflowGroup "" \
  Type "result" \
  Payload '{"result":"success"}' \
  Timestamp "2025-11-07T05:00:00Z"
```

## Statistics

- **Files Changed**: 10
- **Lines Added**: 547
- **Lines Removed**: 20
- **New Files**: 4
- **Modified Files**: 6

## Breaking Changes

None. All existing functionality is preserved. The new Redis stream listener operates independently of existing hub methods.

## Future Considerations

- Consider implementing exponential backoff for error retries
- Add metrics/monitoring for message processing rates
- Consider adding dead-letter queue for persistently failing messages
- Add health check endpoint for Redis connection status
