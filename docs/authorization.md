# Authorization

PrepAPI uses **JWT authentication** with a custom **data-level authorization** pattern. Every user owns their resources (preps, recipes, tags, ratings), and the app enforces this through database-level filtering.

## How It Works

### Authentication Flow
1. Client sends request with **JWT Bearer token**
2. ASP.NET Core validates token
3. **ClaimsTransformation** extracts external identity (`sub` claim) and loads matching `User` entity from database
4. Populates `IUserContext` with current user for dependency injection

### Data Authorization
1. `CheckCurrentUserAuthHandler` verifies user exists via `RequireCurrentUser()` policy
2. EF Core query filters automatically scope all queries using `IUserContext.User.Id` (internal ID)
3. Users can only access their own resources

### Roles

`RequireAdmin()` extends `RequireCurrentUser()` and checks for the "admin" role claim. Admins bypass query filters to access all data.
