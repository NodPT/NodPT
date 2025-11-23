# NodPT.Data

Shared data access layer using DevExpress XPO (eXpress Persistent Objects) for object-relational mapping with MySQL/MariaDB database.

## 🛠️ Technology Stack

- **DevExpress XPO 25.1.3**: Object-Relational Mapping framework
- **.NET 8.0**: Target framework
- **MySQL/MariaDB**: Primary database
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

## 📝 Usage

### Unit of Work Pattern

The recommended approach for data service:

```csharp
public class YourService
{
    private readonly UnitOfWork _unitOfWork;
    private User _user;
    
    // pass the UnitOfWork via DI
    public YourService(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    // RECOMMENDED: Accept ClaimsPrincipal or User in constructor for automatic user validation
    public YourService(UnitOfWork unitOfWork, ClaimsPrincipal claimsPrincipal)
    {
        _unitOfWork = unitOfWork;
        _user = UserService.GetUser(claimsPrincipal, unitOfWork);
        
        // Validate user at constructor level
        if (_user == null)
        {
            throw new UnauthorizedAccessException("User is not authorized or not found");
        }
    }
    
    // Alternative: Accept User directly
    public YourService(UnitOfWork unitOfWork, User user)
    {
        _unitOfWork = unitOfWork;
        _user = user;
        
        if (_user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }
    }
    
    public async Task<DataClass> UpdateData(int id, DataDto dto)
    {
        // User is already validated in constructor, so you can use it directly
        var data = await _unitOfWork.FindObject<DataClass>(id);
        if (data == null)
            throw new KeyNotFoundException("Data not found");
        
        data.DisplayName = dto.DisplayName;
        data.UpdatedAt = DateTime.UtcNow;
        
        data.Save();
        await _unitOfWork.CommitAsync();
        
        return data;
    }
}
```

## 🔑 Authentication & User Management

### Getting Current User from Context

**IMPORTANT**: Always use `UserService.GetUser(ClaimsPrincipal)` to get the current user from the Context.User. **NEVER** require firebaseUid from frontend clients.

```csharp
// In controller or service:
var user = UserService.GetUser(User, unitOfWork);

if (user == null)
{
    // Return unauthorized or throw exception
    throw new UnauthorizedAccessException("User is banned, not approved, or not found");
}

// Use the user object for all operations
user.Projects.Add(newProject);
```

### UserService Methods

```csharp
// Get User entity from ClaimsPrincipal (Context.User)
// Returns null if user is not found, banned, or not approved
User? user = UserService.GetUser(ClaimsPrincipal user, UnitOfWork session);

// Check if user is valid (active, approved, not banned)
bool isValid = UserService.IsUserValid(string firebaseUId, UnitOfWork session);

// Get Firebase UID from ClaimsPrincipal
string? firebaseUid = UserService.GetFirebaseUIDFromContent(ClaimsPrincipal user);
```

### Service Layer Pattern with User Validation

Services should accept `ClaimsPrincipal` or `User` in their constructor to enable automatic user validation at the service layer instead of controller layer:

```csharp
// Example: ProjectService
public class ProjectService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly User _user;
    
    public ProjectService(UnitOfWork unitOfWork, ClaimsPrincipal claimsPrincipal)
    {
        _unitOfWork = unitOfWork;
        _user = UserService.GetUser(claimsPrincipal, unitOfWork);
        
        if (_user == null)
        {
            throw new UnauthorizedAccessException("User not authorized");
        }
    }
    
    public ProjectDto CreateProject(ProjectDto projectDto)
    {
        // No need to pass firebaseUid or validate user here
        // User is already validated in constructor
        
        var project = new Project(_unitOfWork)
        {
            Name = projectDto.Name,
            Description = projectDto.Description,
            User = _user, // Use the validated user
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _unitOfWork.Save(project);
        _unitOfWork.CommitTransaction();
        
        return MapToDto(project);
    }
    
    public List<ProjectDto> GetUserProjects()
    {
        // Get projects for the validated user
        return _user.Projects
            .Where(p => p.IsActive)
            .Select(p => MapToDto(p))
            .ToList();
    }
}
```

### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly UnitOfWork _unitOfWork;
    
    public ProjectsController(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    [HttpPost]
    public IActionResult CreateProject([FromBody] ProjectDto projectDto)
    {
        try
        {
            // Pass User (ClaimsPrincipal) to service constructor
            // Service will validate user automatically
            var service = new ProjectService(_unitOfWork, User);
            
            var project = service.CreateProject(projectDto);
            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "User not authorized" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet]
    public IActionResult GetProjects()
    {
        try
        {
            // Service validates user in constructor
            var service = new ProjectService(_unitOfWork, User);
            
            var projects = service.GetUserProjects();
            return Ok(projects);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "User not authorized" });
        }
    }
}
```

## 🛠️ Development Guidelines

### Best Practices

1. **Always use UserService.GetUser()**: Get user from Context.User, don't accept firebaseUid from clients
2. **Validate at Service Layer**: Pass ClaimsPrincipal to service constructor for automatic validation
3. **Use Unit of Work**: Ensures proper transaction management
4. **Async operations**: Use async methods for all database operations
5. **Proper disposal**: UnitOfWork is scoped, let DI handle disposal
6. **Validation**: Validate data before saving
7. **Indexes**: Add indexes to frequently queried columns
8. **Relationships**: Use XPO associations for foreign keys

### Naming Conventions

- **Tables**: PascalCase, plural (Users, Projects, Workflows)
- **Columns**: PascalCase (DisplayName, CreatedAt)
- **Properties**: PascalCase (user.DisplayName)
- **Foreign Keys**: Singular + Id (OwnerId, ProjectId)

## 🔒 Security

### SQL Injection Prevention

XPO uses parameterized queries automatically:

```csharp
// Safe - XPO handles parameterization
var user = new XPQuery<User>(session)
    .FirstOrDefault(u => u.Email == userInputEmail);
```

### User Authorization

Always check user permissions before performing operations:

```csharp
// Get current user from Context
var currentUser = UserService.GetUser(User, unitOfWork);

if (currentUser == null)
{
    return Unauthorized("User not found or not authorized");
}

// Check if user owns the resource
if (resource.User.Oid != currentUser.Oid && !currentUser.IsAdmin)
{
    return Forbid("You don't have permission to access this resource");
}
```

## 📚 Resources

- [DevExpress XPO Documentation](https://docs.devexpress.com/XPO/1998/express-persistent-objects)
- [XPO Best Practices](https://docs.devexpress.com/XPO/2034/best-practices)
- [MySQL Documentation](https://dev.mysql.com/doc/)
