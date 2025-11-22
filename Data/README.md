# NodPT.Data

Shared data access layer using DevExpress XPO (eXpress Persistent Objects) for object-relational mapping with MySQL/MariaDB database.

## 🛠️ Technology Stack

- **DevExpress XPO 25.1.3**: Object-Relational Mapping framework
- **DevExpress Data 25.1.3**: Data manipulation utilities
- **.NET 8.0**: Target framework
- **MySQL/MariaDB**: Primary database
- **Unit of Work Pattern**: Transaction management
- **Repository Pattern**: Data access abstraction

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

### Project Structure

```
Data/
├── src/
│   ├── Models/             # XPO persistent objects
│   │   ├── User.cs        # User entity
│   │   ├── Project.cs     # Project entity
│   │   ├── Workflow.cs    # Workflow entity
│   │   └── ...            # Other entities
│   ├── Repositories/       # Repository interfaces and implementations
│   │   ├── IRepository.cs         # Generic repository interface
│   │   ├── IUserRepository.cs     # User repository interface
│   │   ├── UserRepository.cs      # User repository implementation
│   │   └── ...                    # Other repositories
│   ├── UnitOfWork/         # Unit of Work pattern
│   │   ├── IUnitOfWork.cs        # UnitOfWork interface
│   │   └── UnitOfWork.cs         # UnitOfWork implementation
│   ├── Configuration/      # Database configuration
│   │   └── DataLayerConfig.cs
│   └── NodPT.Data.csproj  # Project file
└── README.md              # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MySQL 8.0+ or MariaDB 10.5+
- DevExpress Universal Subscription (for XPO license)

### Installation

This is a shared library referenced by other projects:

```xml
<!-- In WebAPI or other project -->
<ItemGroup>
  <ProjectReference Include="..\..\Data\src\NodPT.Data.csproj" />
</ItemGroup>
```

### Database Setup

1. **Create MySQL Database**:
   ```sql
   CREATE DATABASE nodpt 
   CHARACTER SET utf8mb4 
   COLLATE utf8mb4_unicode_ci;
   ```

2. **Create Database User**:
   ```sql
   CREATE USER 'nodpt_user'@'%' IDENTIFIED BY 'secure_password';
   GRANT ALL PRIVILEGES ON nodpt.* TO 'nodpt_user'@'%';
   FLUSH PRIVILEGES;
   ```

3. **Connection String** (in appsettings.json):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=nodpt;User=nodpt_user;Password=secure_password;CharSet=utf8mb4;"
     }
   }
   ```

### Configuration

#### Register in Program.cs (ASP.NET Core)

```csharp
// Configure XPO data layer
builder.Services.AddSingleton<IDataLayer>((serviceProvider) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    XpoDefault.DataLayer = XpoDefault.GetDataLayer(
        connectionString,
        DevExpress.Xpo.DB.AutoCreateOption.DatabaseAndSchema
    );
    return XpoDefault.DataLayer;
});

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register repositories (if needed individually)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
```

## 📝 Usage

### Unit of Work Pattern

The recommended approach for data access:

```csharp
public class YourController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public YourController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        return Ok(users);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserDto dto)
    {
        var user = new User(_unitOfWork.Session)
        {
            FirebaseUid = dto.FirebaseUid,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            CreatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.UserRepository.AddAsync(user);
        await _unitOfWork.CommitAsync();
        
        return Ok(user);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto dto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        
        user.DisplayName = dto.DisplayName;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.UserRepository.UpdateAsync(user);
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

### Repository Interface

```csharp
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(object id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(object id);
    Task<bool> ExistsAsync(object id);
}

public interface IUserRepository : IRepository<User>
{
    Task<User> GetByFirebaseUidAsync(string firebaseUid);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetApprovedUsersAsync();
}
```

### Repository Implementation

```csharp
public class UserRepository : IUserRepository
{
    private readonly Session _session;
    
    public UserRepository(Session session)
    {
        _session = session;
    }
    
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await Task.FromResult(
            new XPQuery<User>(_session).ToList()
        );
    }
    
    public async Task<User> GetByIdAsync(object id)
    {
        return await Task.FromResult(
            _session.GetObjectByKey<User>(id)
        );
    }
    
    public async Task<User> GetByFirebaseUidAsync(string firebaseUid)
    {
        return await Task.FromResult(
            new XPQuery<User>(_session)
                .FirstOrDefault(u => u.FirebaseUid == firebaseUid)
        );
    }
    
    public async Task<User> AddAsync(User entity)
    {
        await _session.SaveAsync(entity);
        return entity;
    }
    
    public async Task<User> UpdateAsync(User entity)
    {
        await _session.SaveAsync(entity);
        return entity;
    }
    
    public async Task DeleteAsync(object id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await _session.DeleteAsync(entity);
        }
    }
}
```

## 🔄 Transactions

### Using Unit of Work for Transactions

```csharp
try
{
    // Start transaction (implicit with UnitOfWork)
    var user = new User(_unitOfWork.Session)
    {
        Email = "user@example.com",
        DisplayName = "John Doe"
    };
    await _unitOfWork.UserRepository.AddAsync(user);
    
    var project = new Project(_unitOfWork.Session)
    {
        Name = "My Project",
        OwnerId = user.Id
    };
    await _unitOfWork.ProjectRepository.AddAsync(project);
    
    // Commit transaction
    await _unitOfWork.CommitAsync();
}
catch (Exception ex)
{
    // Rollback is automatic if CommitAsync is not called
    _logger.LogError(ex, "Transaction failed");
    throw;
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

### Lazy Loading

```csharp
// Navigation properties are lazy-loaded by default
var user = await _unitOfWork.UserRepository.GetByIdAsync(1);

// This will trigger a separate query
foreach (var project in user.Projects)
{
    Console.WriteLine(project.Name);
}
```

### Eager Loading

```csharp
var users = new XPQuery<User>(session)
    .Include(u => u.Projects)
    .ToList();
```

### Aggregations

```csharp
var projectCount = new XPQuery<Project>(session)
    .Count(p => p.OwnerId == userId);

var averageNodeCount = new XPQuery<Workflow>(session)
    .Average(w => w.NodeCount);
```

## 🔒 Security

### SQL Injection Prevention

XPO uses parameterized queries automatically:

```csharp
// Safe - XPO handles parameterization
var user = new XPQuery<User>(session)
    .FirstOrDefault(u => u.Email == userInputEmail);
```

### Validation

```csharp
public class User : XPObject
{
    private string _email;
    
    [Size(255)]
    public string Email
    {
        get => _email;
        set
        {
            if (!IsValidEmail(value))
                throw new ArgumentException("Invalid email format");
            SetPropertyValue(nameof(Email), ref _email, value);
        }
    }
    
    private bool IsValidEmail(string email)
    {
        // Email validation logic
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
```

## 📊 Database Migrations

### Schema Updates

XPO can automatically update the database schema:

```csharp
// Auto create tables if they don't exist
XpoDefault.DataLayer = XpoDefault.GetDataLayer(
    connectionString,
    AutoCreateOption.DatabaseAndSchema
);

// Only create missing tables (don't drop existing)
XpoDefault.DataLayer = XpoDefault.GetDataLayer(
    connectionString,
    AutoCreateOption.SchemaAlreadyExists
);

// No automatic schema changes (production)
XpoDefault.DataLayer = XpoDefault.GetDataLayer(
    connectionString,
    AutoCreateOption.None
);
```

### Manual Migrations

For production, use manual migration scripts:

```sql
-- Add new column
ALTER TABLE Users ADD COLUMN LastLoginAt DATETIME NULL;

-- Create index
CREATE INDEX idx_users_firebase ON Users(FirebaseUid);

-- Add foreign key
ALTER TABLE Projects 
ADD CONSTRAINT fk_projects_owner 
FOREIGN KEY (OwnerId) REFERENCES Users(Id);
```

## 🧪 Testing

### Unit Testing with In-Memory Layer

```csharp
[Fact]
public async Task CanCreateUser()
{
    // Arrange
    var dataLayer = XpoDefault.GetDataLayer(
        InMemoryDataStore.AutoCreateOption.SchemaAlreadyExists
    );
    var uow = new UnitOfWork(dataLayer);
    var repository = new UserRepository(uow);
    
    // Act
    var user = new User(uow)
    {
        Email = "test@example.com",
        DisplayName = "Test User"
    };
    await repository.AddAsync(user);
    await uow.CommitChangesAsync();
    
    // Assert
    var savedUser = await repository.GetByEmailAsync("test@example.com");
    Assert.NotNull(savedUser);
    Assert.Equal("Test User", savedUser.DisplayName);
}
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

### Performance Tips

```csharp
// BAD: N+1 query problem
var users = await _unitOfWork.UserRepository.GetAllAsync();
foreach (var user in users)
{
    var projectCount = user.Projects.Count; // Separate query per user
}

// GOOD: Single query with aggregation
var userProjectCounts = new XPQuery<User>(session)
    .Select(u => new
    {
        User = u,
        ProjectCount = u.Projects.Count
    })
    .ToList();
```

## 🤝 Contributing

### Adding New Entities

1. Create XPO persistent class in `Models/`
2. Add table attribute: `[Persistent("TableName")]`
3. Define properties with appropriate attributes
4. Create repository interface and implementation
5. Add repository to `IUnitOfWork`
6. Update this README

### Code Review Checklist

- [ ] Entity has proper indexes
- [ ] Foreign keys are defined correctly
- [ ] Size limits are set for string fields
- [ ] Validation is implemented where needed
- [ ] Repository interface is defined
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