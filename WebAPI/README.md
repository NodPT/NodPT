# NodPT WebAPI

RESTful API service built with ASP.NET Core and .NET 8. This service provides data management, authentication, and business logic for the NodPT platform.

## 🛠️ Technology Stack

- **.NET 8.0**: Modern .NET framework for high-performance web APIs
- **ASP.NET Core**: Web framework for building APIs
- **DevExpress XPO**: Object-Relational Mapping (ORM) for database access
- **MySQL/MariaDB**: Primary database
- **Redis**: Caching and message streaming
- **Firebase Admin SDK**: Authentication and token validation
- **JWT Bearer Authentication**: Secure API endpoints
- **Swashbuckle**: OpenAPI/Swagger documentation

### Key NuGet Packages

- `DevExpress.Xpo` (25.1.3): ORM framework
- `DevExpress.Data` (25.1.3): Data manipulation utilities
- `FirebaseAdmin` (3.4.0): Firebase authentication
- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0): JWT authentication
- `StackExchange.Redis` (2.9.32): Redis client
- `MySql.Data` (9.1.0): MySQL database provider
- `Swashbuckle.AspNetCore` (6.5.0): API documentation

## 🏗️ Architecture

### Project Structure

```
WebAPI/
├── src/
│   ├── Controllers/       # API endpoints
│   ├── Services/         # Business logic layer
│   ├── Models/           # Data transfer objects
│   ├── Middleware/       # Custom middleware
│   ├── Authentication/   # Auth handlers
│   ├── Program.cs        # Application entry point
│   ├── appsettings.json  # Configuration
│   └── NodPT.API.csproj  # Project file
├── Dockerfile            # Docker container config
├── docker-compose.yml    # Docker Compose config
└── README.md            # This file
```

### Data Access Pattern

The API uses the **Unit of Work** pattern with DevExpress XPO for data access:

```
WebAPI → Controllers → UnitOfWork → Repositories → Database
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MySQL/MariaDB database
- Redis server
- Firebase project (for authentication)
- Docker (for containerized deployment)

### Local Development

1. **Install .NET 8 SDK**:
   Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

2. **Clone and navigate to project**:
   ```bash
   cd WebAPI/src
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Configure appsettings.Development.json**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=nodpt;User=root;Password=yourpassword;"
     },
     "Redis": {
       "ConnectionString": "localhost:6379"
     },
     "Firebase": {
       "ProjectId": "your-firebase-project-id"
     },
     "Jwt": {
       "SecretKey": "your-secret-key-min-32-chars",
       "Issuer": "NodPT.API",
       "Audience": "NodPT.Client"
     }
   }
   ```

5. **Set up Firebase credentials**:
   ```bash
   export GOOGLE_APPLICATION_CREDENTIALS="/path/to/serviceAccountKey.json"
   ```

6. **Run the application**:
   ```bash
   dotnet run
   ```

   The API will be available at `http://localhost:8846`

7. **Access Swagger documentation**:
   Navigate to `http://localhost:8846/swagger`

### Build for Production

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

## 🐳 Docker Deployment

### Environment Setup

Create environment file at `/home/runner_user/envs/backend.env`:

```env
# Database Configuration
DB_HOST=your-mysql-host
DB_PORT=3306
DB_NAME=nodpt
DB_USER=your-db-user
DB_PASSWORD=your-db-password

# Redis Configuration
REDIS_CONNECTION=nodpt-redis:6379

# Firebase Configuration
GOOGLE_APPLICATION_CREDENTIALS=/app/firebase-credentials.json
FIREBASE_PROJECT_ID=your-firebase-project-id

# JWT Configuration
JWT_SECRET_KEY=your-secure-secret-key-minimum-32-characters
JWT_ISSUER=NodPT.API
JWT_AUDIENCE=NodPT.Client

# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8846
ASPNETCORE_HTTP_PORTS=8846

# Application Settings
IS_APPROVED=true
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
```

### Build and Run with Docker

```bash
# Create network if not exists
docker network create backend_network

# Build the image (from repository root)
cd WebAPI
docker-compose build

# Start the container
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

The API will be accessible at `http://localhost:8846`

### Dockerfile Stages

1. **Build Stage**: Restores and builds the .NET project
2. **Publish Stage**: Publishes the release build
3. **Runtime Stage**: Uses ASP.NET Core runtime image (lightweight)

## 📝 Development Guidelines

### Creating Controllers

Always use the Unit of Work pattern for data access:

```csharp
[ApiController]
[Route("api/[controller]")]
public class YourController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public YourController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var items = await _unitOfWork.YourRepository.GetAllAsync();
        return Ok(items);
    }
}
```

### Authentication

Use `CustomAuthorize` attribute to protect endpoints:

```csharp
[CustomAuthorize("Admin")]
[HttpPost]
public async Task<IActionResult> CreateItem([FromBody] ItemDto dto)
{
    // Validate user
    if (!User.IsValidUser(firebaseUid))
    {
        return Unauthorized();
    }
    
    // Your logic here
}
```

### User Validation

Always validate the user in protected endpoints:

```csharp
// Get current user from Firebase token
var user = await UserService.GetUser(User);

if (user == null || user.IsBanned)
{
    return Unauthorized("User is banned or not found");
}

// Proceed with authorized logic
```

### Redis Integration

Inject job data into Redis streams:

```csharp
// Inject Redis connection
private readonly IConnectionMultiplexer _redis;

public async Task QueueJob(JobData job)
{
    var db = _redis.GetDatabase();
    var streamKey = "jobs:manager"; // or jobs:inspector, jobs:agent
    
    await db.StreamAddAsync(streamKey, new NameValueEntry[]
    {
        new("jobId", job.JobId),
        new("workflowId", job.WorkflowId),
        new("task", job.Task),
        new("payload", JsonSerializer.Serialize(job.Payload))
    });
}
```

## 🔐 Authentication & Authorization

### Firebase Authentication

1. Users authenticate via Firebase in the frontend
2. Frontend sends Firebase ID token in Authorization header
3. API validates token using Firebase Admin SDK
4. User information is extracted and stored in HttpContext.User

### JWT Bearer Tokens

The API uses JWT Bearer authentication with Firebase ID tokens:

```http
Authorization: Bearer <firebase-id-token>
```

### User Approval System

- **Banned Users**: Cannot access the API (IsBanned = true)
- **Approved Users**: Have full access (IsApproved = true)
- **Development Mode**: Set `IS_APPROVED=true` to allow all users

## 📊 Database

### DevExpress XPO

The project uses DevExpress XPO as the ORM:

- **Unit of Work**: Transaction management
- **Repositories**: Data access abstraction
- **Persistent Objects**: XPO entity classes

### Database Setup

1. Create MySQL database:
   ```sql
   CREATE DATABASE nodpt CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

2. XPO will automatically create tables on first run

3. For migrations, use XPO schema update features

## 🧪 Testing

```bash
# Run tests (if available)
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🎯 API Endpoints

Access Swagger documentation at `/swagger` for complete API reference.

### Common Endpoints

- `GET /api/health`: Health check
- `POST /api/auth/login`: User authentication
- `GET /api/users/me`: Get current user info
- `GET /api/projects`: List user projects
- `POST /api/workflows`: Create workflow

## ⚡ Performance

### Redis Caching

Use Redis for caching frequently accessed data:

```csharp
var cacheKey = $"user:{userId}";
var cached = await _redis.GetDatabase().StringGetAsync(cacheKey);

if (cached.HasValue)
{
    return JsonSerializer.Deserialize<User>(cached);
}

// Fetch from database and cache
var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
await _redis.GetDatabase().StringSetAsync(cacheKey, 
    JsonSerializer.Serialize(user), 
    TimeSpan.FromMinutes(15));
```

### Database Optimization

- Use async methods for all database operations
- Use projection to select only needed fields
- Implement pagination for large datasets
- Use proper indexing on frequently queried fields

## 🔧 Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=nodpt;..."
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Firebase": {
    "ProjectId": "your-project-id"
  },
  "Jwt": {
    "SecretKey": "your-secret-key",
    "Issuer": "NodPT.API",
    "Audience": "NodPT.Client",
    "ExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

## 🤝 Contributing

1. Follow .NET naming conventions (PascalCase for public members)
2. Use async/await for all I/O operations
3. Add XML documentation comments to public APIs
4. Write unit tests for business logic
5. Use dependency injection for services
6. Keep controllers thin, move logic to services
7. Handle exceptions gracefully with proper status codes

### Code Style

- Use C# 12 features when appropriate
- Follow Microsoft's C# coding conventions
- Use nullable reference types
- Avoid using `var` for primitive types

## 🐛 Troubleshooting

### Common Issues

**Database connection fails**:
- Verify MySQL is running
- Check connection string in environment variables
- Ensure database exists

**Firebase authentication fails**:
- Verify GOOGLE_APPLICATION_CREDENTIALS path
- Check Firebase project configuration
- Ensure service account has proper permissions

**Redis connection fails**:
- Verify Redis is running on specified port
- Check REDIS_CONNECTION environment variable
- Test connection: `redis-cli -h host -p port ping`

**Docker build fails**:
- Ensure Data project is in correct path
- Check Dockerfile COPY paths
- Verify .NET 8 SDK is available

## 📚 Dependencies

This project depends on:
- **NodPT.Data**: Shared data layer with entities and repositories

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check Swagger documentation at `/swagger`
- Review logs in `docker-compose logs`
- Contact the development team
