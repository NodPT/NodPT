# Redis Streams Pipeline Migration

## Summary

This document describes the migration from a mixed Redis communication model (Lists + Pub/Sub) to a unified Redis Streams pipeline with a single shared `RedisService`.

## Changes Made

### 1. Data Project - Shared RedisService

**New Files:**
- `Data/src/Models/RedisModels.cs` - Message envelope, listen options, and stream info models
- Updated `Data/src/Services/RedisService.cs` - Complete rewrite using Redis Streams

**New IRedisService API:**
```csharp
Task<string> Add(string streamKey, IDictionary<string, string> envelope);
ListenHandle Listen(string streamKey, string group, string consumerName, handler, options);
Task Delete(string streamKey, string group, string entryId);
Task<int> ClaimPending(string streamKey, string group, string consumerName, int idleThresholdMs);
Task Trim(string streamKey, long maxLen);
Task<RedisStreamInfo> Info(string streamKey, string? group = null);
Task StopListen(ListenHandle handle);
```

**ChatMessage Model Updates:**
- Added `ConnectionId` property (string, size 255) to store SignalR connection ID

**ChatMessageDto Updates:**
- Added `ConnectionId` property

### 2. WebAPI Project

**Modified Files:**
- `WebAPI/src/Controllers/ChatController.cs`:
  - `SendMessage`: Now requires and persists ConnectionId, uses `RedisService.Add("jobs:chat")` after DB commit
  - `Submit`: Now requires ConnectionId, uses `RedisService.Add("jobs:chat")` after DB commit
  
- `WebAPI/src/Program.cs`:
  - Replaced `RedisStreamListener` and `RedisAIResponseListener` with `SignalRUpdateListener`
  - Fixed logger bug (replaced undefined `logger` with `Console.WriteLine`)

- `WebAPI/src/appsettings.json`:
  - Added Redis Streams configuration section

**New Files:**
- `WebAPI/src/BackgroundServices/SignalRUpdateListener.cs` - Listens to `signalr:updates` stream and forwards to SignalR clients

**Obsolete Files (renamed to .obsolete):**
- `WebAPI/src/BackgroundServices/RedisAIResponseListener.cs.obsolete` (was pub/sub based)
- `WebAPI/src/BackgroundServices/RedisStreamListener.cs.obsolete` (redundant with new SignalRUpdateListener)

### 3. Executor Project

**Modified Files:**
- `Executor/src/Program.cs`:
  - Registered `ChatStreamConsumer` and `ChatStreamWorker`
  - Commented out old `ChatJobConsumer`
  - Added `UnitOfWork` registration for database access
  - Added `using DevExpress.Xpo` for XPO support

- `Executor/src/appsettings.json`:
  - Added Redis Streams configuration section

**New Files:**
- `Executor/src/Consumers/ChatStreamConsumer.cs` - Listens to `jobs:chat` stream, processes with LLM, saves to DB, publishes to `signalr:updates`
- `Executor/src/ChatStreamWorker.cs` - Background worker for ChatStreamConsumer

**Obsolete Files (renamed to .obsolete):**
- `Executor/src/ChatWorker.cs.obsolete` (was list-based)
- `Executor/src/Consumers/ChatJobConsumer.cs.obsolete` (was list-based)
- `Executor/src/Worker.cs.obsolete` (used direct Redis calls instead of shared RedisService)
- `Executor/src/Consumers/RedisConsumer.cs.obsolete` (used direct IDatabase instead of shared RedisService)

### 4. SignalR Project

**Status:** Marked as OBSOLETE

- Updated `SignalR/README.md` with deprecation notice
- SignalR Hub functionality now consolidated into WebAPI
- Project kept for historical reference but should not be deployed

### 5. Documentation

**Updated Files:**
- `Data/README.md` - Added comprehensive RedisService API documentation
- `SignalR/README.md` - Added obsolete notice and migration guide

## Architecture Changes

### Before (Mixed Model)

```
WebAPI ──[ListRightPush]──> Redis List (chat.jobs)
                               │
                               ▼
Executor ──[ListLeftPop]─── Redis List
        │
        └──[PublishAsync]──> Redis Pub/Sub (AI.RESPONSE)
                               │
                               ▼
WebAPI ──[SubscribeAsync]── Redis Pub/Sub
        │
        └──> SignalR Hub
```

### After (Unified Streams)

```
WebAPI ──[Add]──> Redis Stream (jobs:chat)
                       │
                       ▼
Executor ──[Listen]─── Redis Stream
        │
        └──[Add]──> Redis Stream (signalr:updates)
                       │
                       ▼
WebAPI ──[Listen]──── Redis Stream
        │
        └──> SignalR Hub (in WebAPI)
```

## Benefits

1. **Single Source of Truth**: One `RedisService` implementation used by all services - ALL Redis Streams operations go through the shared service
2. **Reliability**: Consumer groups ensure no message loss, automatic claiming of stale messages
3. **Scalability**: Multiple consumers can process messages in parallel
4. **Observability**: XPENDING, XINFO commands provide visibility into message processing
5. **Dead Letter Handling**: Failed messages moved to `{streamKey}:dead` after max retries
6. **Simplified Architecture**: SignalR Hub consolidated into WebAPI (one less service to deploy)
7. **Consistency**: All Redis Streams code follows the same pattern - no direct `IDatabase` usage

**Note on Consistency:** The old `Worker` and `RedisConsumer` classes were marked obsolete because they used direct `IDatabase.StreamReadGroupAsync()` calls instead of the unified `RedisService`. For true consistency, any future consumers for `jobs:manager`, `jobs:inspector`, or `jobs:agent` streams should be rewritten to use `RedisService.Listen()` similar to `ChatStreamConsumer`.

## Migration Notes

### Database Migration Required

The `ChatMessage` table requires a migration to add the `ConnectionId` column:

```sql
ALTER TABLE ChatMessage ADD COLUMN ConnectionId VARCHAR(255);
```

XPO will handle this automatically on first run if auto-migration is enabled.

### Frontend Changes Required

Frontend must send `ConnectionId` in chat requests:

**Before:**
```javascript
{
  "NodeId": "node123",
  "Message": "Hello AI",
  "Sender": "user"
}
```

**After:**
```javascript
{
  "NodeId": "node123",
  "Message": "Hello AI",
  "Sender": "user",
  "ConnectionId": "signalr-connection-id-here"
}
```

### Configuration Required

All services need Redis stream configuration in `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Streams": {
      "JobsChat": "jobs:chat",
      "SignalRUpdates": "signalr:updates",
      "TrimMaxLength": 10000
    }
  }
}
```

### Deployment Order

1. Deploy Data project changes (shared library)
2. Deploy WebAPI with new SignalRUpdateListener
3. Deploy Executor with new ChatStreamConsumer
4. **Do not deploy** SignalR project (obsolete)

### Backward Compatibility

**Breaking Changes:**
- Frontend must provide `ConnectionId` in chat requests
- Old list-based queue (`chat.jobs`) will not be consumed
- Old pub/sub channel (`AI.RESPONSE`) will not be consumed
- SignalR service is no longer used

**Migration Strategy:**
- Deploy all services simultaneously
- Drain old Redis lists before migration if needed
- Frontend must be updated to send ConnectionId

## Operational Changes

### Monitoring

New metrics to monitor:
- Stream length: `XLEN jobs:chat`, `XLEN signalr:updates`
- Pending messages: `XPENDING jobs:chat executor`
- Dead letter streams: `XLEN jobs:chat:dead`

### Troubleshooting

**Check pending messages:**
```bash
redis-cli XPENDING jobs:chat executor
```

**Claim stuck messages manually:**
```bash
redis-cli XCLAIM jobs:chat executor consumer-1 3600000 <message-id>
```

**Inspect dead letter stream:**
```bash
redis-cli XRANGE jobs:chat:dead - +
```

### Cleanup

After successful migration, the following can be removed:
- Old Redis list: `chat.jobs`
- Old pub/sub channel: `AI.RESPONSE`
- SignalR Docker container/service
- Obsolete `.obsolete` files in Executor and WebAPI projects:
  - `Executor/src/Worker.cs.obsolete`
  - `Executor/src/ChatWorker.cs.obsolete`
  - `Executor/src/Consumers/RedisConsumer.cs.obsolete`
  - `Executor/src/Consumers/ChatJobConsumer.cs.obsolete`
  - `WebAPI/src/BackgroundServices/RedisAIResponseListener.cs.obsolete`
  - `WebAPI/src/BackgroundServices/RedisStreamListener.cs.obsolete`

## Testing

### Build Status
✅ Data project builds successfully  
✅ WebAPI project builds successfully  
✅ Executor project builds successfully  

### Code Quality
✅ Code review completed - all issues addressed  
✅ Security scan completed - no vulnerabilities found  

### Manual Testing
⚠️ Requires running infrastructure (Redis, MySQL, Ollama)
- [ ] Test chat message flow end-to-end
- [ ] Verify SignalR receives updates
- [ ] Confirm database persistence
- [ ] Test failure scenarios and retries

## Security Summary

**CodeQL Scan Results:** ✅ No alerts found

**Security Improvements:**
- Minimal data in Redis streams (just IDs and connectionId)
- Database remains source of truth for sensitive data
- Proper error handling prevents data exposure in logs
- Structured logging prevents log injection

**Security Considerations:**
- Ensure Redis connections use TLS in production
- Configure Redis authentication
- Do not log full message payloads
- ConnectionId validation prevents unauthorized access

## Future Enhancements

1. **Metrics & Monitoring**: Add Prometheus metrics for stream operations
2. **Admin UI**: Web interface to view pending messages and dead letters
3. **Auto-scaling**: Scale Executor consumers based on pending message count
4. **Stream Archival**: Move old messages to cold storage after processing
5. **Multi-tenancy**: Separate streams per customer/tenant

## References

- [Redis Streams Documentation](https://redis.io/docs/data-types/streams/)
- [StackExchange.Redis Streams API](https://stackexchange.github.io/StackExchange.Redis/Streams)
- Problem Statement: See issue description for detailed requirements
