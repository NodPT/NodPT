📄 Overview
This project builds a visual AI-assisted workflow editor backend using web api .net8
The goal is to create a clean, modular web api.
Data models will be simple, focusing on nodes and other data. Using Entity Framework Core ORM database for persistence.

Keep the code clean, modular, and easy to understand. Use mock data for initial development. don't overthink the logic, keep it simple.

## 🔐 CRITICAL: User Authentication Pattern

**ALWAYS use UserService.GetUser to get the current user from Context.User**

```csharp
// ✅ CORRECT: Get user from Context.User (ClaimsPrincipal)
var user = UserService.GetUser(User, dbContext);
if (user == null)
{
    return Unauthorized(new { error = "User not found or invalid" });
}

// Use user.Id for database operations
var userProjects = await dbContext.Projects
    .Where(p => p.UserId == user.Id)
    .ToListAsync();

// ❌ INCORRECT: Never require firebaseUid from clients
[HttpGet("user/{firebaseUid}")]  // DON'T DO THIS
public IActionResult GetUserData(string firebaseUid) { ... }

// ✅ CORRECT: Get user from token
[HttpGet("me")]
public IActionResult GetMyData()
{
    var user = UserService.GetUser(User, dbContext);
    if (user == null)
        return Unauthorized();
    // ... use user.Id
}
```

**Key Rules:**
1. **Never require firebaseUid from clients** - Always extract it from the authenticated user's claims (Context.User)
2. **Always validate the user** - Use `UserService.GetUser(User, context)` which returns null if user is not active, approved, or is banned
3. **Return 401 Unauthorized** if user is null
4. **Security first** - The firebaseUid in the JWT token is the source of truth, not client-provided values

Most controllers need to get User from UserService.GetUser to update data of that user accordingly to the controller's related data models.

## 🧱 Backend Setup Guide (ASP.NET Core Web API)

### 🛠️ Step 1: Create a New ASP.NET Core Web API Project

### 📁 Step 2: Organize Project Structure

Create folders to keep things clean:

```
/Controllers
/Models
/Services
/Data
```

---

### 🧩 Step 3: Define Models

Create simple models to simulate data:

**Models/Node.cs**

```csharp
public class Node: XPLiteObject
{
    public string Id { get; set; }
    public string Name { get; set; }
    public NodeType NodeType { get; set; } // e.g., "active", "locked"
    public Dictionary<string, string> Properties { get; set; }
    public XPCollection<ChatMessage> ChatMessages { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } // e.g., "active", "locked"
    public Node? PanelRawNode { get; set; } // For grouping nodes
    public XPCollection<Node> Children { get; set; } // for grouping nodes
}
```

**Models/ChatMessage.cs**

```csharp
public class ChatMessage: XPObject
{
    public string Sender { get; set; } // user or AI
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool MarkedAsSolution { get; set; } // For marking messages as solutions
    public Node Node { get; set; } // Reference to the node this message belongs
    public bool Liked { get; set; } // For user feedback
    public bool Disliked { get; set; } // For user feedback
}
```

---

### 🧠 Step 4: Create Mock Services

**Services/NodeService.cs**

```csharp
public class NodeService
{
    private static List<Node> _nodes = new()
    {
        new Node { Id = "1", Name = "Start", Status = "active", Properties = new() },
        new Node { Id = "2", Name = "Process", Status = "locked", Properties = new() }
    };

    public List<Node> GetAllNodes() => _nodes;
    public Node? GetNode(string id) => _nodes.FirstOrDefault(n => n.Id == id);
}
```

**Services/ChatService.cs**

```csharp
public class ChatService
{
    private static List<ChatMessage> _messages = new();

    public List<ChatMessage> GetMessages() => _messages;

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
    }
}
```

---

### 🧭 Step 5: Create Controllers

**Controllers/NodesController.cs**

```csharp
[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly NodeService _nodeService = new();

    [HttpGet]
    public IActionResult GetNodes() => Ok(_nodeService.GetAllNodes());

    [HttpGet("{id}")]
    public IActionResult GetNode(string id)
    {
        var node = _nodeService.GetNode(id);
        return node == null ? NotFound() : Ok(node);
    }
}
```

**Controllers/ChatController.cs**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService = new();

    [HttpGet]
    public IActionResult GetMessages() => Ok(_chatService.GetMessages());

    [HttpPost]
    public IActionResult PostMessage([FromBody] ChatMessage message)
    {
        _chatService.AddMessage(message);
        return Ok();
    }
}
```

---

### 🔧 Step 6: Enable CORS for Frontend

In `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
```

---

### 🚀 Step 8: Run and Test

1. Run the backend.
2. Use Postman or your Vue frontend to test endpoints:
   - `GET /api/nodes`
   - `GET /api/nodes/{id}`
   - `GET /api/chat`
   - `POST /api/chat`

---

### ✅ Final Notes

- Keep logic simple and clean.
- Use mock data until real database or AI integration is needed.
- You can later add endpoints for:
  - Build progress
  - Timeline rollback
  - Node grouping/locking
  - AI tool interactions

Would you like me to generate a sample `.http` file or Postman collection to test these endpoints?

---

### 📊 Logging Pattern

All controllers should implement error logging using the following pattern:

**Models/Log.cs**

```csharp
public class Log : XPObject
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Username { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
}
```

**Controllers Pattern with Dependency Injection**

Controllers should use dependency injection for DbContext and wrap all methods with try-catch blocks. 
**CRITICAL: Always use UserService.GetUser(User, dbContext) to get the authenticated user.**

```csharp
[ApiController]
[Route("api/[controller]")]
[CustomAuthorized]  // Ensure authentication is required
public class ProjectsController : ControllerBase
{
    private readonly NodPTDbContext dbContext;

    public ProjectsController(NodPTDbContext _dbContext)
    {
        this.dbContext = _dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProjects()
    {
        try
        {
            // ✅ Get user from Context.User (ClaimsPrincipal)
            var user = UserService.GetUser(User, dbContext);
            if (user == null)
            {
                return Unauthorized(new { error = "User not found or invalid" });
            }
            
            // Query user's projects using user.Id
            var projects = await dbContext.Projects
                .Where(p => p.UserId == user.Id)
                .ToListAsync();
            
            return Ok(projects);
        }
        catch (Exception ex)
        {
            LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetMyProjects");
            return StatusCode(500, new { error = "An error occurred." });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] ProjectDto dto)
    {
        try
        {
            // ✅ Get user from token, not from client
            var user = UserService.GetUser(User, dbContext);
            if (user == null)
            {
                return Unauthorized(new { error = "User not found or invalid" });
            }
            
            var project = new Project
            {
                Name = dto.Name,
                UserId = user.Id,  // ✅ Use user from token
                CreatedAt = DateTime.UtcNow
            };
            
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();
            
            return Ok(project);
        }
        catch (Exception ex)
        {
            LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "CreateProject");
            return StatusCode(500, new { error = "An error occurred." });
        }
    }
}
```

**Service Registration**

Register services in Program.cs:

```csharp
builder.Services.AddScoped<LogService>();
```

**LogController**

The LogController exposes all logged errors via GET request:

```csharp
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetLogs() => Ok(_logService.GetAllLogs());
}
```

Access logs at: `GET /api/logs`
