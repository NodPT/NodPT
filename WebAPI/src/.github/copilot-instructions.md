📄 Overview
This project builds a visual AI-assisted workflow editor backend using web api .net9
The goal is to create a clean, modular web api.
Data models will be simple, focusing on nodes and other data. Using XPO ORM database for persistence.

Keep the code clean, modular, and easy to understand. Use mock data for initial development. don't overthink the logic, keep it simple.

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

Controllers should use dependency injection for UnitOfWork and LogService, and wrap all methods with try-catch blocks:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly UnitOfWork unitOfWork;
    private readonly LogService _logService;

    public ProjectsController(UnitOfWork _unitOfWork, LogService logService)
    {
        this.unitOfWork = _unitOfWork;
        this._logService = logService;
    }

    [HttpGet]
    public IActionResult GetProjects()
    {
        try
        {
            // Your logic here
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "GetProjects");
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

---

### 🔑 Authentication & User Management

**IMPORTANT**: Always use `UserService.GetUser(User, unitOfWork)` to get the current user from Context.User. **NEVER** require firebaseUid from frontend clients. The user's identity is extracted from the JWT token in the Authorization header.

### Getting Current User

```csharp
// Get current user from Context.User (ClaimsPrincipal)
var user = UserService.GetUser(User, unitOfWork);

if (user == null)
{
    return Unauthorized(new { error = "User is banned, not approved, or not found" });
}

// Use the user object for all operations
user.Projects.Add(newProject);
await unitOfWork.CommitAsync();
```

### Service Layer Pattern

**RECOMMENDED**: Controllers should pass `User` (ClaimsPrincipal) to service constructors. Services will validate users at construction time, eliminating the need for validation in controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly UnitOfWork unitOfWork;
    
    public ProjectsController(UnitOfWork _unitOfWork)
    {
        this.unitOfWork = _unitOfWork;
    }
    
    [HttpPost]
    public IActionResult CreateProject([FromBody] ProjectDto projectDto)
    {
        try
        {
            // Service validates user in constructor
            var service = new ProjectService(unitOfWork, User);
            var project = service.CreateProject(projectDto);
            return Ok(project);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            LogService.LogError(ex.Message, ex.StackTrace, User?.Identity?.Name, "ProjectsController", "CreateProject");
            return StatusCode(500, new { error = "An error occurred." });
        }
    }
    
    [HttpGet]
    public IActionResult GetProjects()
    {
        try
        {
            // Service validates user and returns their projects
            var service = new ProjectService(unitOfWork, User);
            var projects = service.GetUserProjects();
            return Ok(projects);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
```

### No FirebaseUid from Frontend

Controllers should **NOT** accept firebaseUid as a parameter from the frontend. Instead, extract it from Context.User:

```csharp
// ❌ BAD - Don't do this
[HttpGet("user/{firebaseUid}")]
public IActionResult GetProjectsByUser(string firebaseUid)
{
    // This allows users to access other users' data
}

// ✅ GOOD - Do this instead
[HttpGet]
public IActionResult GetProjects()
{
    try
    {
        var service = new ProjectService(unitOfWork, User);
        var projects = service.GetUserProjects();
        return Ok(projects);
    }
    catch (UnauthorizedAccessException)
    {
        return Unauthorized(new { error = "User not authorized" });
    }
}
```
