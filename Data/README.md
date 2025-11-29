# NodPT.Data

Shared data access layer using DevExpress XPO (eXpress Persistent Objects) for object-relational mapping with MySQL/MariaDB database. **Also provides shared RedisService for unified Redis Streams communication across all services.**

## 🛠️ Technology Stack

- **DevExpress XPO 25.1.3**: Object-Relational Mapping framework
- **.NET 8.0**: Target framework
- **MySQL/MariaDB**: Primary database
- **StackExchange.Redis 2.9.32**: Redis client for Streams support
- **Unit of Work Pattern**: Transaction management

## 🏗️ Architecture

### Data Access Layers

```
Controllers/Services
    │
    ▼
Unit of Work
    │
    ├─→ User Repository
    ├─→ Project Repository
    ├─→ Workflow Repository
    ├─→ Node Repository
    └─→ ... other repositories
    │
    ▼
XPO Session
    │
    ▼
MySQL Database
```

### Redis Streams Communication

The shared `RedisService` provides a unified interface for all Redis Streams operations across WebAPI, Executor, and other services:

```
WebAPI ──┐
         ├──→ IRedisService (shared) ──→ Redis Streams
Executor─┘
```

**Key Features:**
- Single source of truth for Redis operations
- Consumer groups with automatic claiming of stale messages
- Retry logic with dead-letter stream support
- XADD, XREADGROUP, XACK, XDEL, XTRIM operations
- Background listeners with configurable concurrency

### Redis Service Architecture

The Redis functionality is organized into two logical categories:

```
IRedisService (unified interface)
    ├── IRedisQueueService (message queue operations)
    │   ├── Add()           # Publish message to stream
    │   ├── Listen()        # Subscribe to stream
    │   ├── Acknowledge()   # Confirm message processed
    │   ├── ClaimPending()  # Recover stale messages
    │   ├── Trim()          # Limit stream size
    │   ├── Info()          # Get stream stats
    │   └── StopListen()    # Stop listener
    │
    └── IRedisCacheService (caching operations)
        ├── Get()           # Get cached value
        ├── Set()           # Cache value with optional expiry
        ├── Exists()        # Check if key exists
        ├── Remove()        # Delete cached value
        ├── Update()        # Append to list
        ├── Range()         # Get list range
        ├── TrimList()      # Limit list size
        └── Length()        # Get list length
```

**Interface Selection Guide:**
- Use `IRedisQueueService` for message queuing (WebAPI → Executor → SignalR)
- Use `IRedisCacheService` for caching summaries and chat history
- Use `IRedisService` for backward compatibility or mixed usage

### Project Structure

```
Data/
├── src/
│   ├── Models/             # XPO persistent objects + Redis models
│   │   ├── User.cs        # User entity
│   │   ├── Project.cs     # Project entity
│   │   ├── ChatMessage.cs # Chat message with ConnectionId
│   │   ├── RedisModels.cs # MessageEnvelope, ListenOptions, etc.
│   │   └── ...            # Other entities
│   ├── Interfaces/        # Service interfaces
│   │   ├── IRedisService.cs       # Unified Redis interface
│   │   ├── IRedisQueueService.cs  # Queue operations interface
│   │   ├── IRedisCacheService.cs  # Cache operations interface
│   │   └── ...                    # Other interfaces
│   ├── Services/          # Data services and Redis service
│   │   ├── RedisService.cs      # Shared Redis implementation
│   │   ├── ChatService.cs       # Chat service
│   │   └── ...                  # Other service classes
│   ├── DTOs/              # Data Transfer Objects
│   │   ├── ChatMessageDto.cs    # Chat DTO with ConnectionId
│   │   └── ...                  # Other DTO classes
│   ├── Attributes/        # Custom attributes
│   └── NodPT.Data.csproj  # Project file
└── README.md              # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MySQL 8.0+ or MariaDB 10.5+
- Redis 7.0+ (with Streams support)
- DevExpress XPO (free)

### Installation

This is a shared library referenced by other projects:

```xml
<!-- In WebAPI or Executor project -->
<ItemGroup>
  <ProjectReference Include="..\..\Data\src\NodPT.Data.csproj" />
</ItemGroup>
```

### Database Setup

1. **Connection String** (in appsettings.json):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=nodpt;User=nodpt_user;Password=secure_password;CharSet=utf8mb4;"
     }
   }
   ```

## 📝 Usage
### Unit of Work Pattern
The recommended approach for data service:

```csharp
public class YourService
{
    private readonly UnitOfWork _unitOfWork;
    private User _user;
    
    // pass the UnitOfWork via DI
    public YourController(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
     // pass the UnitOfWork via DI
    public YourController(User user)
    {
        _user = user;
        _unitOfWork = user.unitOfWork;
    }
    
    
    public async DataClass UpdateData(int id, DataDto dto)
    {
        var data = await _unitOfWork.FindObjec<DataClass>(id);
        if (data == null)
            return NotFound();
        
        data.DisplayName = dto.DisplayName;
        data.UpdatedAt = DateTime.UtcNow;
        
        data.Save(user);
        await _unitOfWork.CommitAsync();
        
        return Ok(user);
    }
}
```

### Creating XPO Entities

```csharp
using DevExpress.Xpo;
using System;

[Persistent("Users")]
public class User : XPObject
{
    public User(Session session) : base(session) { }
    
    [Key(AutoGenerate = true)]
    public int Id { get; set; }
    
    [Indexed(Unique = true)]
    [Size(128)]
    public string FirebaseUid { get; set; }
    
    [Size(255)]
    public string Email { get; set; }
    
    [Size(255)]
    public string DisplayName { get; set; }
    
    public bool IsApproved { get; set; }
    
    public bool IsBanned { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [Association("User-Projects")]
    public XPCollection<Project> Projects
    {
        get { return GetCollection<Project>(nameof(Projects)); }
    }
}
```



## 🎯 XPO Features

### Querying with LINQ

```csharp
// Simple query
var activeUsers = new XPQuery<User>(session)
    .Where(u => !u.IsBanned && u.IsApproved)
    .ToList();

// Complex query with joins
var projectsWithOwners = new XPQuery<Project>(session)
    .Select(p => new
    {
        ProjectName = p.Name,
        OwnerName = p.Owner.DisplayName,
        CreatedAt = p.CreatedAt
    })
    .ToList();
```


## 🔒 Security

### SQL Injection Prevention

XPO uses parameterized queries automatically:

```csharp
// Safe - XPO handles parameterization
var user = new XPQuery<User>(session)
    .FirstOrDefault(u => u.Email == userInputEmail);
```



## 🛠️ Development Guidelines

### Best Practices

1. **Always use Unit of Work**: Ensures proper transaction management
2. **Async operations**: Use async methods for all database operations
3. **Proper disposal**: UnitOfWork is scoped, let DI handle disposal
4. **Validation**: Validate data before saving
5. **Indexes**: Add indexes to frequently queried columns
6. **Relationships**: Use XPO associations for foreign keys

### Naming Conventions

- **Tables**: PascalCase, plural (Users, Projects, Workflows)
- **Columns**: PascalCase (DisplayName, CreatedAt)
- **Properties**: PascalCase (user.DisplayName)
- **Foreign Keys**: Singular + Id (OwnerId, ProjectId)


## 🤝 Contributing

### Adding New Entities

1. Create XPO persistent class in `Models/`
2. Add table attribute: `[Persistent("TableName")]`
3. Define properties with appropriate attributes
6. Update this README

### Code Review Checklist

- [ ] Entity has proper indexes
- [ ] Foreign keys are defined correctly
- [ ] Size limits are set for string fields
- [ ] Validation is implemented where needed
- [ ] Unit tests are added
- [ ] Documentation is updated

## 📚 Resources

- [DevExpress XPO Documentation](https://docs.devexpress.com/XPO/1998/express-persistent-objects)
- [XPO Best Practices](https://docs.devexpress.com/XPO/2034/best-practices)
- [MySQL Documentation](https://dev.mysql.com/doc/)

## 🐛 Troubleshooting

### Common Issues

**Connection fails**:
- Verify MySQL is running
- Check connection string format
- Ensure user has proper permissions

**Schema not updating**:
- Check `AutoCreateOption` setting
- Verify database user has ALTER permissions
- Use manual migration for production

**Slow queries**:
- Add indexes to frequently queried columns
- Use `Include()` for eager loading
- Check query execution plan

**Memory leaks**:
- Ensure Sessions are properly disposed
- Use scoped `UnitOfWork` with DI
- Don't hold references to entities outside scope

## 📞 Support

For issues and questions:
- Check DevExpress documentation
- Open an issue on GitHub
- Contact the development team

## 📡 RedisService API

The Redis functionality is split into two interfaces for better separation of concerns:

### Interfaces

| Interface | Purpose | Methods |
|-----------|---------|---------|
| `IRedisQueueService` | Message queuing between services | Add, Listen, Acknowledge, ClaimPending, Trim, Info, StopListen |
| `IRedisCacheService` | Caching summaries and history | Get, Set, Exists, Remove, Update, Range, TrimList, Length |
| `IRedisService` | Unified interface (backward compatible) | All methods from both interfaces |

### Queue Operations (IRedisQueueService)

#### Add - Publish to Stream
```csharp
Task<string> Add(string streamKey, IDictionary<string, string> envelope)
```
Adds a message to a Redis Stream using XADD. Returns the entry ID.

**Example:**
```csharp
var envelope = new Dictionary<string, string>
{
    { "chatId", "123" },
    { "connectionId", "abc-xyz" },
    { "timestamp", DateTime.UtcNow.ToString("o") }
};
var entryId = await _redisService.Add("jobs:chat", envelope);
```

#### Listen - Subscribe to Stream
```csharp
ListenHandle Listen(string streamKey, string group, string consumerName, 
    Func<MessageEnvelope, CancellationToken, Task<bool>> handler, 
    ListenOptions? options = null)
```
Starts listening to a Redis Stream with consumer group. Handler returns `true` for success (will XACK), `false` for retry.

**Example:**
```csharp
var handle = _redisService.Listen(
    streamKey: "jobs:chat",
    group: "executor",
    consumerName: "executor-worker-1",
    handler: async (envelope, ct) =>
    {
        // Process message
        var chatId = envelope.Fields["chatId"];
        
        // Return true to acknowledge, false to retry
        return true;
    },
    options: new ListenOptions
    {
        BatchSize = 10,
        Concurrency = 3,
        MaxRetries = 3
    });
```

#### Acknowledge - Acknowledge Message
```csharp
Task Acknowledge(string streamKey, string group, string entryId)
```
Acknowledges a message using XACK, marking it as successfully processed.

#### ClaimPending - Reclaim Stale Messages
```csharp
Task<int> ClaimPending(string streamKey, string group, string consumerName, int idleThresholdMs)
```
Claims messages that have been idle for too long (failed consumers).

#### Trim - Limit Stream Size
```csharp
Task Trim(string streamKey, long maxLen)
```
Trims the stream to approximately maxLen messages using XTRIM.

#### Info - Get Stream Metadata
```csharp
Task<RedisStreamInfo> Info(string streamKey, string? group = null)
```
Returns stream length, total pending, and per-consumer pending counts.

#### StopListen - Stop Listener
```csharp
Task StopListen(ListenHandle handle)
```
Gracefully stops a listener started with Listen().

### Cache Operations (IRedisCacheService)

#### Get - Retrieve Cached Value
```csharp
Task<string?> Get(string key)
```
Gets a string value from Redis, returns null if not found.

#### Set - Cache a Value
```csharp
Task Set(string key, string value, TimeSpan? expiry = null)
```
Stores a string value with optional expiration.

#### Exists - Check Key Existence
```csharp
Task<bool> Exists(string key)
```
Returns true if the key exists in Redis.

#### Remove - Delete Cached Value
```csharp
Task<bool> Remove(string key)
```
Deletes a key, returns true if deleted.

#### Update - Append to List
```csharp
Task<long> Update(string key, string value)
```
Appends a value to a Redis list, returns new length.

#### Range - Get List Range
```csharp
Task<List<string>> Range(string key, long start = 0, long stop = -1)
```
Gets elements from a list. Supports negative indices.

#### TrimList - Limit List Size
```csharp
Task TrimList(string key, long start, long stop)
```
Keeps only elements in the specified range.

#### Length - Get List Length
```csharp
Task<long> Length(string key)
```
Returns the number of elements in the list.

### Stream Keys (Convention)

- `jobs:chat` - Chat processing jobs (WebAPI → Executor)
- `signalr:updates` - Real-time updates (Executor → WebAPI)
- `{streamKey}:dead` - Dead letter stream for failed messages

### Cache Keys (Convention)

- `node:summary:{nodeId}` - Conversation summaries
- `node:history:{nodeId}` - Chat history lists

### Consumer Groups

- `executor` - Executor service consumers
- `signalr` - WebAPI SignalR listeners

### Configuration

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

### ListenOptions

```csharp
var options = new ListenOptions
{
    BatchSize = 10,              // Messages per read
    Concurrency = 3,             // Parallel handlers
    ClaimIdleThresholdMs = 60000, // Claim after 1 minute idle
    MaxRetries = 3,              // Retries before dead letter
    PollDelayMs = 1000,          // Delay when no messages
    CreateStreamIfMissing = true, // Auto-create stream/group
    ClaimPendingOnStartup = true  // Claim on startup
};
```

### Dependency Injection

```csharp
// All three interfaces resolve to the same RedisService instance
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<IRedisQueueService>(sp => sp.GetRequiredService<IRedisService>());
builder.Services.AddSingleton<IRedisCacheService>(sp => sp.GetRequiredService<IRedisService>());
```

**Usage in services:**
```csharp
// For queue-only operations
public class ChatWorker
{
    private readonly IRedisQueueService _queue;
    public ChatWorker(IRedisQueueService queue) => _queue = queue;
}

// For cache-only operations
public class MemoryService
{
    private readonly IRedisCacheService _cache;
    public MemoryService(IRedisCacheService cache) => _cache = cache;
}

// For mixed operations (backward compatible)
public class LegacyService
{
    private readonly IRedisService _redis;
    public LegacyService(IRedisService redis) => _redis = redis;
}
```

### Error Handling

- Failed handlers return `false` → message is retried
- After `MaxRetries` → message moves to `{streamKey}:dead`
- Dead letter stream preserves original data + failure metadata
- Pending messages are auto-claimed by healthy consumers

