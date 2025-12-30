# Verification Guide for MySQL SSL Fix

This guide helps verify that the MySQL SSL connection fix resolves the authentication errors in the Executor service.

## Pre-requisites

1. Docker and Docker Compose installed
2. Backend environment file configured at `/home/runner_user/envs/backend.env`
3. Required environment variables set:
   - `DB_HOST`
   - `DB_PORT`
   - `DB_NAME`
   - `DB_USER`
   - `DB_PASSWORD`
   - `REDIS_CONNECTION`

## Verification Steps

### 1. Check Database Connection String

Verify the connection strings now use `SslMode=None`:

```bash
# Executor
grep -n "SslMode" Executor/src/Services/DatabaseInitializer.cs

# WebAPI  
grep -n "SslMode" WebAPI/src/Services/DatabaseInitializer.cs
```

Expected output should show `SslMode=None` (not `SslMode=Preferred`).

### 2. Build the Services

```bash
# Build Executor
cd Executor/src
dotnet restore
dotnet build

# Build WebAPI
cd ../../WebAPI/src
dotnet restore
dotnet build
```

Both builds should succeed without errors.

### 3. Deploy and Monitor

```bash
# Rebuild and deploy Executor
cd Executor
docker-compose build
docker-compose up -d

# Check logs for successful startup
docker logs nodpt-executor -f
```

### 4. Expected Log Messages

When the Executor starts successfully, you should see:

```
BackendExecutor starting with configuration:
  Redis Connection: nodpt-redis:6379
  LLM Endpoint: http://ollama:11434/api/generate
  ...
ChatStreamWorker starting...
ChatStreamWorker is now listening to jobs:chat stream
```

**No MySQL SSL or authentication errors should appear.**

### 5. End-to-End Test

1. **Send a chat message through the WebAPI:**

```bash
# Example curl request (adjust token and nodeId)
curl -X POST https://api.nodpt.com/api/chat/send \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nodeId": "YOUR_NODE_ID",
    "message": "Hello, test message",
    "connectionId": "test-connection-123"
  }'
```

2. **Monitor Executor logs for processing:**

```bash
docker logs nodpt-executor --tail 50 -f
```

Expected log sequence:
```
=== Processing Redis Job Entry ===
Processing chat job: ChatId={chatId}
Retrieved chat message for chatId {chatId}: {length} chars
Template found: TemplateId={templateId}, Name={templateName}
=== Sending Request to LLM ===
=== Received AI Response ===
Saved AI response: NewChatId={newChatId} for original chatId {chatId}
Updated memory with user message for node {nodeId}
Updated memory with AI response for node {nodeId}
=== Published Result to SignalR ===
=== Chat Job Completed Successfully ===
```

**The key is that no I/O errors or SSL-related exceptions should occur.**

### 6. Verify Redis Stream Operations

Check that messages flow through Redis successfully:

```bash
# Connect to Redis container
docker exec -it nodpt-redis redis-cli

# Check jobs:chat stream
XINFO STREAM jobs:chat

# Check signalr:updates stream  
XINFO STREAM signalr:updates

# List recent messages in jobs:chat
XREVRANGE jobs:chat + - COUNT 5

# List recent messages in signalr:updates
XREVRANGE signalr:updates + - COUNT 5
```

You should see entries in both streams with no errors.

## Common Issues and Solutions

### Issue: Connection timeout to MySQL

**Symptom:** Logs show timeout errors connecting to database

**Solution:** Verify `DB_HOST` points to correct container name (e.g., `nodpt-data`) and that the database container is running:

```bash
docker ps | grep nodpt-data
```

### Issue: Redis connection refused

**Symptom:** Logs show Redis connection errors

**Solution:** Verify Redis is running and accessible:

```bash
docker ps | grep nodpt-redis
docker exec nodpt-redis redis-cli PING
```

Expected output: `PONG`

### Issue: Old SSL errors still appearing

**Symptom:** Still seeing SSL handshake errors in logs

**Solution:** Ensure you've rebuilt the Docker images after the fix:

```bash
cd Executor
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## Success Criteria

The fix is successful when:

- ✅ Executor starts without database authentication errors
- ✅ Chat messages are processed end-to-end
- ✅ Database operations (save, commit) complete successfully  
- ✅ Redis Add operations complete without I/O errors
- ✅ SignalR receives the AI responses through Redis streams
- ✅ No SSL-related exceptions in the logs

## Rollback Plan

If issues persist, you can temporarily rollback:

```bash
# Restore previous connection string with SSL
# In DatabaseInitializer.cs, change:
# SslMode=None
# back to:
# SslMode=Preferred

# Or use environment variable override:
# Add to backend.env:
# DB_CONNECTION_STRING="XpoProvider=MySql;server=nodpt-data;port=3306;user=root;password=xxx;database=nodpt;SslMode=Required;..."
```

However, the root issue should be addressed rather than rolling back.

## Additional Resources

- See `EXECUTOR_DATABASE_SSL_FIX.md` for detailed technical explanation
- MySQL SSL mode documentation: https://dev.mysql.com/doc/connector-net/en/connector-net-connection-options.html
- DevExpress XPO connection strings: https://docs.devexpress.com/XPO/2123/connect-to-a-data-store
