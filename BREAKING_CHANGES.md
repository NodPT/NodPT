# Breaking Changes: DevExpress XPO to Entity Framework Core Migration

## Overview
This document outlines the breaking changes introduced when migrating from DevExpress XPO to Entity Framework Core in the NodPT project.

## Date
**Migration Date:** November 22, 2025

## Affected Components
- **Data Layer** (`Data/src`)
- **WebAPI** (`WebAPI/src`)
- **Executor** (`Executor/src`) - If applicable

## Major Breaking Changes

### 1. Database Context Change

#### Before (DevExpress XPO)
```csharp
using DevExpress.Xpo;

public class ProjectService
{
    private readonly UnitOfWork session;
    
    public ProjectService(UnitOfWork unitOfWork)
    {
        this.session = unitOfWork;
    }
}
```

#### After (Entity Framework Core)
```csharp
using Microsoft.EntityFrameworkCore;
using NodPT.Data;

public class ProjectService
{
    private readonly NodPTDbContext context;
    
    public ProjectService(NodPTDbContext dbContext)
    {
        this.context = dbContext;
    }
}
```

### 2. Model Base Classes

#### Before (DevExpress XPO)
```csharp
using DevExpress.Xpo;

public class User : XPObject
{
    private string? _name;
    
    public User(Session session) : base(session) { }
    
    [Size(255)]
    public string? Name
    {
        get => _name;
        set => SetPropertyValue(nameof(Name), ref _name, value);
    }
}
```

#### After (Entity Framework Core)
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [MaxLength(255)]
    public string? Name { get; set; }
}
```

### 3. Primary Keys

#### Before (DevExpress XPO)
- XPO automatically generates `Oid` property of type `int` for `XPObject` derived classes
- For `XPLiteObject`, you define your own key using `[Key]` attribute

```csharp
// Accessing the ID
var userId = user.Oid;
```

#### After (Entity Framework Core)
- Explicitly define `Id` property with `[Key]` attribute
- Use `[DatabaseGenerated]` for auto-increment behavior

```csharp
// Accessing the ID
var userId = user.Id;
```

### 4. Relationships

#### Before (DevExpress XPO)
```csharp
[Association("User-Projects")]
public User? User
{
    get => _user;
    set => SetPropertyValue(nameof(User), ref _user, value);
}

[Association("User-Projects")]
public XPCollection<Project> Projects => GetCollection<Project>(nameof(Projects));
```

#### After (Entity Framework Core)
```csharp
public int? UserId { get; set; }

[ForeignKey(nameof(UserId))]
public virtual User? User { get; set; }

public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
```

### 5. Querying Data

#### Before (DevExpress XPO)
```csharp
// Using XPCollection
var projects = new XPCollection<Project>(session);

// Using CriteriaOperator
var user = session.FindObject<User>(CriteriaOperator.Parse("FirebaseUid=?", firebaseUid));

// Using GetObjectByKey
var project = session.GetObjectByKey<Project>(id);
```

#### After (Entity Framework Core)
```csharp
// Using DbSet
var projects = context.Projects.ToList();

// Using LINQ
var user = context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);

// Using Find
var project = context.Projects.FirstOrDefault(p => p.Id == id);

// With eager loading
var project = context.Projects
    .Include(p => p.User)
    .Include(p => p.Template)
    .FirstOrDefault(p => p.Id == id);
```

### 6. Transactions

#### Before (DevExpress XPO)
```csharp
session.BeginTransaction();
try
{
    // Database operations
    session.Save(entity);
    session.CommitTransaction();
}
catch
{
    session.RollbackTransaction();
    throw;
}
```

#### After (Entity Framework Core)
```csharp
using var transaction = context.Database.BeginTransaction();
try
{
    // Database operations
    context.SaveChanges();
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### 7. Creating and Updating Entities

#### Before (DevExpress XPO)
```csharp
var user = new User(session)
{
    Name = "John Doe",
    Email = "john@example.com"
};
session.Save(user);
```

#### After (Entity Framework Core)
```csharp
var user = new User
{
    Name = "John Doe",
    Email = "john@example.com"
};
context.Users.Add(user);
context.SaveChanges();
```

### 8. Dependency Injection

#### Before (DevExpress XPO)
```csharp
// In Program.cs
builder.Services.AddXpoDefaultUnitOfWork(true, options =>
    options.UseConnectionString(connectionString));

// In Controller
public ProjectsController(UnitOfWork unitOfWork)
{
    this.unitOfWork = unitOfWork;
}
```

#### After (Entity Framework Core)
```csharp
// In Program.cs
builder.Services.AddDbContext<NodPTDbContext>(options =>
{
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion);
});

// In Controller
public ProjectsController(NodPTDbContext dbContext)
{
    this.dbContext = dbContext;
}
```

### 9. Connection String Format

#### Before (DevExpress XPO)
```csharp
connectionString = $"XpoProvider=MySql;server={host};port={port};user={user};password={password};database={db};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
```

#### After (Entity Framework Core)
```csharp
connectionString = $"Server={host};Port={port};Database={db};User={user};Password={password};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
```

### 10. NotMapped Properties

#### Before (DevExpress XPO)
```csharp
[NonPersistent]
[Browsable(false)]
public AIModel? MatchingAIModel
{
    get { /* computed property */ }
}
```

#### After (Entity Framework Core)
```csharp
[NotMapped]
[Browsable(false)]
public AIModel? MatchingAIModel
{
    get { /* computed property */ }
}
```

## Migration Checklist

When migrating existing code:

- [ ] Replace `UnitOfWork` with `NodPTDbContext`
- [ ] Replace `XPObject`/`XPLiteObject` base classes with plain classes
- [ ] Add `[Key]` and `[DatabaseGenerated]` attributes to Id properties
- [ ] Replace property backing fields and `SetPropertyValue` with auto-properties
- [ ] Replace `[Size(...)]` with `[MaxLength(...)]`
- [ ] Replace `[Association(...)]` with `[ForeignKey(...)]`
- [ ] Replace `XPCollection<T>` with `ICollection<T>` or `List<T>`
- [ ] Replace `session.FindObject<T>` with `context.Set<T>().FirstOrDefault()`
- [ ] Replace `session.GetObjectByKey<T>` with `context.Set<T>().Find()` or `FirstOrDefault()`
- [ ] Replace `session.Query<T>()` with `context.Set<T>()`
- [ ] Replace `session.Save()` with `context.Add()` or update tracking
- [ ] Replace `session.Delete()` with `context.Remove()`
- [ ] Add `.Include()` for eager loading of navigation properties
- [ ] Update transaction handling
- [ ] Replace `[NonPersistent]` with `[NotMapped]`
- [ ] Update connection string format
- [ ] Remove constructor with `Session` parameter
- [ ] Update service instantiation to use `DbContext` instead of `UnitOfWork`

## Database Schema Changes

### Minimal Impact
Entity Framework Core will generate a similar schema to DevExpress XPO. However, be aware of:

1. **Primary Key Names**: EF Core uses `Id` instead of `Oid`
2. **Foreign Key Columns**: Explicitly named (e.g., `UserId` instead of implicit)
3. **Index Creation**: May need manual configuration for complex indexes

### Migration Strategy
```bash
# Create initial migration
dotnet ef migrations add InitialMigration --project Data/src --startup-project WebAPI/src

# Apply migration to database
dotnet ef database update --project Data/src --startup-project WebAPI/src
```

## Testing Recommendations

1. **Unit Tests**: Update unit tests to use EF Core InMemory provider
2. **Integration Tests**: Test against actual MySQL database
3. **Data Validation**: Verify all relationships and constraints work correctly
4. **Performance Testing**: Compare query performance between XPO and EF Core

## Known Issues and Considerations

1. **Lazy Loading**: EF Core requires explicit `Include()` for related entities
2. **Change Tracking**: EF Core tracks changes automatically; be mindful of performance
3. **Concurrency**: Implement optimistic concurrency if needed using `[Timestamp]`
4. **Transactions**: Always use transactions for multi-step operations

## Support and Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Pomelo MySQL Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Migration Guide](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

## Rollback Plan

If issues arise:

1. Revert to previous commit using Git
2. Restore database from backup
3. Review logs for migration errors
4. Contact development team for assistance

---

**Last Updated:** November 22, 2025
**Version:** 1.0.0
