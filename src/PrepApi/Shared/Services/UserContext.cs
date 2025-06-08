using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Users;

namespace PrepApi.Shared.Services;

public interface IUserContext
{
    public User? User { get; set; }
    string ExternalId { get; }
    ClaimsPrincipal Principal { get; set; }
    Guid? InternalId { get; }
}

public class UserContext : IUserContext
{
    public User? User { get; set; }
    public string ExternalId { get; }
    public ClaimsPrincipal Principal { get; set; } = default!;
    public Guid? InternalId => User?.Id;

}

public static class CurrentUserExtensions
{
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        // services.AddScoped<CurrentUser>();
        services.AddScoped<IClaimsTransformation, ClaimsTransformation>();
        return services;
    }
    
    private sealed class ClaimsTransformation(IUserContext currentUser, PrepDb db) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // We're not going to transform anything. We're using this as a hook into authorization
            // to set the current user without adding custom middleware.
            currentUser.Principal = principal;
            
            if (principal.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } externalId)
            {
                // Resolve the user from database and store it on the current user.
                currentUser.User = await db.Users
                    .FirstOrDefaultAsync(u => u.ExternalId == externalId);
            }
            
            return principal;
        }
    }
}