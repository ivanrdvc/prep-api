# User Authorization in PrepAPI

## Summary

PrepAPI uses **Auth0** for authentication with a custom **data-level authorization** pattern. Every user owns their resources (preps, recipes, tags, ratings), and the app enforces this through database-level filtering.

## How It Works

### Authentication Flow
1. Client sends request with **JWT Bearer token** from Auth0
2. ASP.NET Core validates token against Auth0 domain/audience
3. **ClaimsTransformation** middleware extracts Auth0 `sub` (subject ID)
4. Looks up user in database by `ExternalId` (Auth0 sub)
5. Populates `IUserContext` with current user
6. Endpoint handlers access user via dependency injection

### Authorization Pattern
- **Route-level**: Endpoints use `.RequireAuthorization()` or `.RequireAuthorization("CurrentUser")`
- **Policy-based**: `CurrentUser` policy verifies user exists in database (not just valid token)
- **Data-level**: Every query filters by `userId == userContext.InternalId`
- **Ownership checks**: Returns 403 if user not found, 404 if resource doesn't belong to user

### Key Files

| File | Purpose |
|------|---------|
| `Shared/Services/UserContext.cs` | Current user context with role support (IsAdmin, ExternalId) |
| `Shared/Services/UserContextExtensions.cs` | Claims transformation, DI registration |
| `Authorization/CheckCurrentUserAuthHandler.cs` | Authorization handler + policy extensions |
| `Users/UserEndpoints.cs` | User CRUD operations |
| `Program.cs:44-56` | Auth0 JWT & authorization configuration |

## Data Model

```
Auth0 (sub) ──┐
              │
              ▼
User.ExternalId (unique Auth0 identifier)
User.Id (internal GUID) ──┐
                          │
                          ├──► Preps.UserId
                          ├──► Recipes.UserId
                          ├──► Tags.UserId
                          ├──► PrepRatings.UserId
                          └──► Ingredients.UserId (nullable - allows shared ingredients)
```

## Authorization Examples

### Creating a User
```csharp
// POST /api/users - UserEndpoints.cs:22
var externalId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
var user = new User { ExternalId = externalId! };
```

### Fetching User's Preps
```csharp
// GET /api/preps - PrepEndpoints.cs:110
var prep = await db.Preps
    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
```

### Shared Ingredients Pattern
```csharp
// Ingredients can be global (UserId == null) or user-owned
var ingredients = await db.Ingredients
    .Where(i => i.UserId == null || i.UserId == userId)
    .ToListAsync();
```

## Authorization Handler Pattern

We now use ASP.NET Core's **IAuthorizationHandler** for policy-based authorization:

### CurrentUser Policy
```csharp
// Program.cs:55-56
builder.Services.AddAuthorizationBuilder()
    .AddCurrentUserHandler();
```

### How It Works
1. `RequireCurrentUser()` policy requires both:
   - Authenticated user (valid JWT token)
   - User exists in database (`userContext.User is not null`)
2. If user not found → **403 Forbidden** (not 404)
3. If user found → Handler succeeds, request proceeds

### Usage in Endpoints
```csharp
// Apply to entire group
var group = routes.MapGroup("/api/preps");
group.WithTags("Preps");
group.RequireAuthorization(pb => pb.RequireCurrentUser());

// Or apply to individual endpoints
group.MapGet("/me", GetCurrentUser)
    .RequireAuthorization(pb => pb.RequireCurrentUser());
group.MapPost("/", CreateCurrentUser)
    .RequireAuthorization(); // Just authenticated, user doesn't exist yet
```

### Enhanced IUserContext
```csharp
public interface IUserContext
{
    User? User { get; }                    // Database user entity
    ClaimsPrincipal Principal { get; }     // Claims from JWT
    Guid? InternalId { get; }              // User.Id shortcut
    string? ExternalId { get; }            // Auth0 sub claim
    bool IsAdmin { get; }                  // Role check
}
```

## Current Security Approach

✓ **JWT validation** via Auth0
✓ **Route protection** on all endpoints
✓ **Policy-based authorization** (CurrentUser handler)
✓ **Ownership filtering** in all queries
✓ **Database-level** foreign keys enforce relationships
✓ **403 responses** when user doesn't exist
✓ **Role support** (IsAdmin property ready for RBAC)

~ **Partial RBAC** - IsAdmin property available, not yet used
✗ No permission-based authorization
✗ No resource sharing between users
✗ No audit logging for authorization failures

## Role-Based Authorization

### Admin Role Support

The `IUserContext` now includes role checking:

```csharp
public interface IUserContext
{
    bool IsAdmin { get; }  // Checks if Principal has "admin" role
}
```

### Using Admin Policy

```csharp
// AuthorizationExtensions.cs provides RequireAdmin()
group.MapDelete("/users/{id}", DeleteUser)
    .RequireAuthorization(pb => pb.RequireAdmin());

// RequireAdmin = RequireCurrentUser + RequireRole("admin")
```

### How Auth0 Roles Work

1. Configure roles in Auth0 dashboard
2. Add role claims to JWT tokens
3. ASP.NET Core maps roles automatically from claims
4. Use `userContext.IsAdmin` or `.RequireAdmin()` in endpoints

## Code Organization

All authorization code is now in `src/PrepApi/Authorization/`:

```
Authorization/
└── CheckCurrentUserAuthHandler.cs  # Handler + policy extensions (RequireCurrentUser, RequireAdmin)
```

## Recent Improvements (2025-10-26)

1. **Authorization Handler Pattern** - Added `CurrentUserAuthorizationHandler` for proper 403 responses
2. **Enhanced UserContext** - Added `ExternalId` and `IsAdmin` properties
3. **Policy-based Authorization** - Created reusable `CurrentUser` and `Admin` policies
4. **Removed Redundant Code** - GetCurrentUser and UpdateCurrentUser no longer query DB
5. **Organized Authorization** - Moved all auth code to Authorization folder
6. **Applied Policies** - All endpoints now use `RequireCurrentUser()` policy

## Future Improvements

1. **Resource Sharing** - Allow users to share recipes/preps with others
2. **Permission-based Authorization** - Fine-grained permissions (read, write, admin)
3. **Audit Logging** - Track authorization failures and admin actions
4. **User Lockout** - Implement the TODO in CheckCurrentUserAuthHandler.cs:33
5. **EF Core Query Filters** - Global filters to automatically scope data by user
