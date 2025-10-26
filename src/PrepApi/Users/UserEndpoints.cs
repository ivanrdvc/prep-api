using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using PrepApi.Authorization;
using PrepApi.Data;
using PrepApi.Users.Requests;

namespace PrepApi.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users");
        group.WithTags("Users");

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization(pb => pb.RequireCurrentUser());
        group.MapPut("/me", UpdateCurrentUser)
            .RequireAuthorization(pb => pb.RequireCurrentUser());

        group.MapPost("/", CreateCurrentUser)
            .RequireAuthorization();

        return group;
    }

    public static Task<Ok<UserDto>> GetCurrentUser(IUserContext userContext)
    {
        // User is guaranteed to exist by RequireCurrentUser policy
        // Already loaded by ClaimsTransformation, no DB query needed
        return Task.FromResult(TypedResults.Ok(UserDto.FromUser(userContext.User!)));
    }

    public static async Task<Results<Created<UserDto>, Conflict>> CreateCurrentUser(
        PrepDb db,
        IUserContext userContext,
        IHttpContextAccessor httpContextAccessor)
    {
        var claimsPrincipal = httpContextAccessor.HttpContext?.User;
        if (claimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (userContext.User is not null)
        {
            return TypedResults.Conflict();
        }

        var userId = Guid.NewGuid();

        var newUser = new User
        {
            Id = userId,
            ExternalId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value!,
            PreferredUnits = PreferredUnits.Metric,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var userDto = UserDto.FromUser(newUser);

        return TypedResults.Created("/api/users/me", userDto);
    }

    public static async Task<Results<NoContent, ValidationProblem>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        var user = userContext.User!;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PreferredUnits = request.PreferredUnits;

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}