# Brief

# Development
## Controller Guide
- create constructor with inject unitOfWork. Eg.:
```csharp	
private readonly IUnitOfWork _unitOfWork;
public YourController(IUnitOfWork unitOfWork)
{
	_unitOfWork = unitOfWork;
}
```

- use _unitOfWork to access repositories. Eg.:
```csharp
var items = await _unitOfWork.YourRepository.GetAllAsync();
```

## Dependencies:
- Add NodPT.Data project reference to your project to access data models and services.
- Database is MySQL/MariaDB. 
- Using DevExpress.XPO and DevExpress.Data for ORM and data access.

## Authentication:
- Using JWT Bearer authentication.
- Firebase is used for token generation and validation.
- Use CustomAuthorize to protect your endpoints. Eg.:
```csharp
[CustomAuthorize("Admin")]
public async Task<IActionResult> YourProtectedEndpoint()
{
	// Your code here
}
```
- use User.IsValidUser(string firebaseUid) to validate user before performing actions.
- Banned users are restricted from accessing the API.
- Approved users have full access to the API. Set IsApproved as true if your product is ready for public.
- Add Firebase Admin SDK to your project for user management and Login validation.

## Deployment:
- Add VITE_FIREBASE_{choice and name} to the git secrets for firebase configuration.
- Update docker-compose.yml with your database and firebase settings.
