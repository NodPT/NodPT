# Testing Redis Integration

This directory contains test utilities for the Redis stream integration.

## TestRedisProducer

A test utility that simulates the Executor writing messages to the Redis stream `signalr:updates`.

### Running the Test

1. Ensure Redis is running (default: localhost:6379)
2. Start the SignalR hub server: `dotnet run`
3. Connect a client to the SignalR hub (use monitor.html or test-client.html)
4. Run the test producer:

```bash
cd Tests
dotnet run TestRedisProducer.cs
```

Or specify a custom Redis connection:

```bash
dotnet run TestRedisProducer.cs redis:6379
```

### What it does

The test producer writes three sample messages to the `signalr:updates` stream with different routing criteria:

1. **Message 1**: Routes to `test-user-1` via userId
2. **Message 2**: Routes to `workflow-alpha` group
3. **Message 3**: Routes to `test-user-3` via userId

### Using with Docker Compose

If you're running the system with Docker Compose, the Redis service is typically named `redis`, so use:

```bash
dotnet run TestRedisProducer.cs redis:6379
```

## Manual Testing with redis-cli

You can also manually add messages using `redis-cli`:

```bash
redis-cli XADD signalr:updates * \
  MessageId "test-msg-001" \
  NodeId "node-test" \
  ProjectId "project-test" \
  UserId "test-user-1" \
  ClientConnectionId "" \
  WorkflowGroup "" \
  Type "result" \
  Payload '{"result":"success","data":"Manual test"}' \
  Timestamp "2025-11-07T05:00:00Z"
```

## Verifying Messages

To see messages in the Redis stream:

```bash
# View all messages in the stream
redis-cli XREAD COUNT 10 STREAMS signalr:updates 0

# View consumer group info
redis-cli XINFO GROUPS signalr:updates

# View pending messages
redis-cli XPENDING signalr:updates signalr-hub-group
```
