using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;

namespace PrepApi.Shared.Services;

public static class UserContextExtensions
{
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IClaimsTransformation, ClaimsTransformation>();

        return services;
    }

    private sealed class ClaimsTransformation(IUserContext userContext, PrepDb db) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Not transforming anything, using this as a hook into authorization 
            // to set the current user without adding custom middleware.
            userContext.Principal = principal;

            if (principal.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } externalId)
            {
                userContext.User = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
            }

            return principal;
        }
    }
}