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

### Project Structure

```
Data/
├── src/
│   ├── Models/             # XPO persistent objects
│   │   ├── User.cs        # User entity
│   │   ├── Project.cs     # Project entity
│   │   └── ...            # Other entities
│   ├── Services/       # Data services and Unit of Work
│   │   └── ...                    # Service classes
│   ├── Attributes/       # Custom attributes
│   │   └── ...                    # Attribute classes
│   ├── Dto/      # Data Transfer Objects
│   │   └── ... # DTO classes
│   └── NodPT.Data.csproj  # Project file
└── README.md              # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MySQL 8.0+ or MariaDB 10.5+
- DevExpress XPO (free)

### Installation

This is a shared library referenced by other projects:

```xml
<!-- In WebAPI or other project -->
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