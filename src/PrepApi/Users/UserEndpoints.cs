using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Shared.Services;

namespace PrepApi.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUser);
        group.MapPost("/", CreateCurrentUser);
        group.MapPut("/me", UpdateCurrentUser);

        return group;
    }

    public static async Task<Results<Ok<UserDto>, NotFound>> GetCurrentUser(
        PrepDb db,
        IUserContext userContext)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userContext.InternalId);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(UserDto.FromUser(user));
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

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userContext.InternalId);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PreferredUnits = request.PreferredUnits;

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}