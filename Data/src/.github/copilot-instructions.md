# NodPT.Data - Copilot Instructions

## 🛠️ Technology Stack

- **Entity Framework Core**: Object-Relational Mapping framework
- **.NET 8.0**: Target framework
- **MySQL/MariaDB**: Primary database
- **Unit of Work Pattern**: Transaction management
- **Repository Pattern**: Data access abstraction

## 🏗️ Architecture

The data layer uses Entity Framework Core with the Unit of Work and Repository patterns for clean data access.

```
Controllers/Services
    │
    ▼
DbContext (NodPTDbContext)
    │
    ▼
MySQL Database
```

## 📝 Core Principles

### User Authentication Pattern

**CRITICAL: Always use UserService.GetUser to get the current user from Context.User**

```csharp
// ✅ CORRECT: Get user from Context.User (ClaimsPrincipal)
var user = UserService.GetUser(User, context);
if (user == null)
{
    return Unauthorized(new { error = "User not found or invalid" });
}

// ❌ INCORRECT: Never require firebaseUid from clients
// Don't accept firebaseUid as a parameter from API requests
```

**Key Rules:**
1. **Never require firebaseUid from clients** - Always extract it from the authenticated user's claims (Context.User)
2. **Always validate the user** - Use `UserService.GetUser(User, context)` which returns null if user is not active, approved, or is banned
3. **Return appropriate errors** - Return 401 Unauthorized if user is null
4. **Security first** - The firebaseUid in the JWT token is the source of truth, not client-provided values

### UserService Methods

```csharp
// Get user from ClaimsPrincipal (Context.User in controllers)
public static User? GetUser(ClaimsPrincipal user, NodPTDbContext context)
{
    string? firebaseUid = GetFirebaseUIDFromContent(user);
    if (string.IsNullOrEmpty(firebaseUid))
        return null;
    
    var dbUser = context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);
    if (dbUser != null && dbUser.Active && dbUser.Approved && !dbUser.Banned)
        return dbUser;
    
    return null;
}

// Extract Firebase UID from claims
public static string? GetFirebaseUIDFromContent(ClaimsPrincipal user)
{
    if (!user.Identity?.IsAuthenticated ?? true)
        return null;
    
    return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("user_id")?.Value
        ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? user.FindFirst("sub")?.Value;
}
```

## 🎯 Entity Framework Core Usage

### DbContext Pattern

```csharp
public class YourController : ControllerBase
{
    private readonly NodPTDbContext _context;
    
    public YourController(NodPTDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        // Get current authenticated user
        var user = UserService.GetUser(User, _context);
        if (user == null)
            return Unauthorized(new { error = "User not found or invalid" });
        
        // Query data for this user
        var items = await _context.Items
            .Where(i => i.UserId == user.Id)
            .ToListAsync();
        
        return Ok(items);
    }
}
```

### CRUD Operations

**Create (Add)**
```csharp
[HttpPost]
public async Task<IActionResult> CreateItem([FromBody] ItemDto dto)
{
    // Get authenticated user from token
    var user = UserService.GetUser(User, _context);
    if (user == null)
        return Unauthorized(new { error = "User not found or invalid" });
    
    var item = new Item
    {
        Name = dto.Name,
        UserId = user.Id,  // Use user from token
        CreatedAt = DateTime.UtcNow
    };
    
    _context.Items.Add(item);
    await _context.SaveChangesAsync();
    
    return Ok(item);
}
```

**Read (Query)**
```csharp
// Get single entity
var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);

// Get with related entities (eager loading)
var project = await _context.Projects
    .Include(p => p.User)
    .Include(p => p.Nodes)
    .FirstOrDefaultAsync(p => p.Id == projectId);

// Get with filtering for current user
var user = UserService.GetUser(User, _context);
var userItems = await _context.Items
    .Where(i => i.UserId == user.Id)
    .ToListAsync();
```

**Update**
```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemDto dto)
{
    // Get authenticated user
    var user = UserService.GetUser(User, _context);
    if (user == null)
        return Unauthorized(new { error = "User not found or invalid" });
    
    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
    if (item == null)
        return NotFound();
    
    // Verify ownership
    if (item.UserId != user.Id)
        return Forbid();
    
    item.Name = dto.Name;
    item.UpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    return Ok(item);
}
```

**Delete**
```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteItem(int id)
{
    // Get authenticated user
    var user = UserService.GetUser(User, _context);
    if (user == null)
        return Unauthorized(new { error = "User not found or invalid" });
    
    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
    if (item == null)
        return NotFound();
    
    // Verify ownership
    if (item.UserId != user.Id)
        return Forbid();
    
    _context.Items.Remove(item);
    await _context.SaveChangesAsync();
    
    return NoContent();
}
```

### Transactions

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    var user = UserService.GetUser(User, _context);
    if (user == null)
        return Unauthorized();
    
    _context.Items.Add(newItem);
    _context.Projects.Add(newProject);
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## 🔒 Security Best Practices

1. **Always validate user from token**: Never trust client-provided user identifiers
2. **Check ownership**: Verify the authenticated user owns the resource before modifying
3. **Return appropriate HTTP status codes**:
   - 401 Unauthorized: User not authenticated or invalid
   - 403 Forbidden: User doesn't have permission
   - 404 Not Found: Resource doesn't exist
4. **Use parameterized queries**: EF Core handles this automatically
5. **Validate input**: Always validate DTOs before processing

## 🛠️ Development Guidelines

### Best Practices

1. **Always use UserService.GetUser**: Extract user from Context.User, never from client parameters
2. **Async operations**: Use async methods for all database operations
3. **Proper disposal**: DbContext is scoped, let DI handle disposal
4. **Validation**: Validate data before saving
5. **Indexes**: Add indexes to frequently queried columns
6. **Relationships**: Use EF Core navigation properties

### Naming Conventions

- **Tables**: PascalCase, plural (Users, Projects, Nodes)
- **Columns**: PascalCase (DisplayName, CreatedAt)
- **Properties**: PascalCase (user.DisplayName)
- **Foreign Keys**: Singular + Id (UserId, ProjectId)

### Common Patterns

```csharp
// Pattern 1: Get current user and their data
var user = UserService.GetUser(User, _context);
if (user == null)
    return Unauthorized(new { error = "User not found or invalid" });

var userProjects = await _context.Projects
    .Where(p => p.UserId == user.Id)
    .ToListAsync();

// Pattern 2: Verify ownership before update/delete
var user = UserService.GetUser(User, _context);
if (user == null)
    return Unauthorized();

var item = await _context.Items.FindAsync(id);
if (item == null)
    return NotFound();

if (item.UserId != user.Id)
    return Forbid();

// Pattern 3: Create with authenticated user
var user = UserService.GetUser(User, _context);
if (user == null)
    return Unauthorized();

var newItem = new Item
{
    UserId = user.Id,
    // ... other properties
};
```

## 📊 Database Migrations

### Creating a Migration
```bash
cd /home/runner/work/NodPT/NodPT/WebAPI/src
dotnet ef migrations add MigrationName --project ../../Data/src
```

### Applying Migrations
```bash
cd /home/runner/work/NodPT/NodPT/WebAPI/src
dotnet ef database update --project ../../Data/src
```

### Removing the Last Migration
```bash
cd /home/runner/work/NodPT/NodPT/WebAPI/src
dotnet ef migrations remove --project ../../Data/src
```

## 🔑 Key Takeaways

1. ✅ **DO** use `UserService.GetUser(User, context)` to get the current user
2. ✅ **DO** validate that user is not null before proceeding
3. ✅ **DO** check user ownership of resources
4. ✅ **DO** use async/await for all database operations
5. ❌ **DON'T** accept firebaseUid from client requests
6. ❌ **DON'T** trust client-provided user identifiers
7. ❌ **DON'T** skip user validation checks
8. ❌ **DON'T** use synchronous database operations
