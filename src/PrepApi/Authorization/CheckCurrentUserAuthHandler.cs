using Microsoft.AspNetCore.Authorization;

namespace PrepApi.Authorization;

public static class AuthorizationHandlerExtensions
{
    public static AuthorizationBuilder AddCurrentUserHandler(this AuthorizationBuilder builder)
    {
        builder.Services.AddScoped<IAuthorizationHandler, CheckCurrentUserAuthHandler>();
        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCurrentUser(this AuthorizationPolicyBuilder builder)
    {
        return builder.RequireAuthenticatedUser()
            .AddRequirements(new CheckCurrentUserRequirement());
    }

    public static AuthorizationPolicyBuilder RequireAdmin(this AuthorizationPolicyBuilder builder)
    {
        return builder.RequireCurrentUser()
            .RequireRole("admin");
    }

    private class CheckCurrentUserRequirement : IAuthorizationRequirement;

    // This authorization handler verifies that the user exists in the database even if there's a valid JWT token
    private class CheckCurrentUserAuthHandler(IUserContext userContext) : AuthorizationHandler<CheckCurrentUserRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CheckCurrentUserRequirement requirement)
        {
            if (userContext.User is not null)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}